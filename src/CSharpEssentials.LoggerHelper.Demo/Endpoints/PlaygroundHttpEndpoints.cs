using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog.Events;
using System.Diagnostics;
using static CSharpEssentials.LoggerHelper.Demo.Endpoints.PlaygroundEndpoints;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 12: HttpHelper nel Playground — chiamate VERE a un'API pubblica gratuita (httpbin.org di default,
/// configurabile con "Playground:HttpBaseUrl", es. un httpbin in Docker per lavorare offline).
/// Ogni scenario configura un httpsClientHelper al volo (retry Polly, timeout, rate limit, Bearer) e logga ogni
/// tentativo con il logger fluente del Playground: stessi sink, stesso masking scelti nella pagina.
/// </summary>
public class PlaygroundHttpEndpoints : IEndpointDefinition {
    public static readonly string[] Scenarios = ["success", "flaky", "down", "timeout", "bearer", "post", "ratelimit"];

    // ponytail: one shared connection pool; each request wraps it in its own logging handler + events,
    // so per-request callbacks never accumulate on a shared client.
    private static readonly SocketsHttpHandler Sockets = new() { PooledConnectionLifetime = TimeSpan.FromMinutes(2) };

    public sealed record PlaygroundHttpRequest(
        string Scenario,
        Dictionary<string, string> Sinks,
        PlaygroundMasking? Masking = null,
        int Retries = 3,
        double Backoff = 1,
        int TimeoutSeconds = 10,
        int Burst = 5,
        int PermitLimit = 2,
        string? BearerToken = null);

    private sealed record Call(HttpMethod Method, string Path, object? Body = null, Dictionary<string, string>? Headers = null);

    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/playground").WithTags("Playground");

        group.MapPost("/http", async (PlaygroundHttpRequest req, IConfiguration config, LoggerHelperOptions hostOptions,
                                      ILogErrorStore hostErrors, CancellationToken ct) => {
            if (!Scenarios.Contains(req.Scenario))
                return Results.BadRequest(new { error = $"Unknown scenario '{req.Scenario}'. Use: {string.Join(", ", Scenarios)}" });
            if (req.Sinks is not { Count: > 0 })
                return Results.BadRequest(new { error = "Select at least one sink" });

            var cfg = config.GetSection("Playground");
            var baseUrl = (cfg["HttpBaseUrl"] ?? "https://httpbin.org").TrimEnd('/');

            await Gate.WaitAsync(ct);
            try {
                var session = new Session(req.Sinks, req.Masking, renderedMessage: true, fileNameProperty: null, cfg, hostOptions, hostErrors);
                List<object> calls;
                var attempts = new List<object>();
                try {
                    calls = await RunScenario(req, baseUrl, session.Logger, attempts, ct);
                } finally {
                    session.Dispose();
                }
                return Results.Ok(new { scenario = req.Scenario, calls, attempts, log = session.Report() });
            } finally {
                Gate.Release();
            }
        })
        .WithSummary("Call a free public API through HttpHelper and log every attempt to the chosen sinks")
        .WithDescription(
            "Scenarios against httpbin.org: success (GET /get), flaky (random 500/502/503/200), down (always 503), " +
            "timeout (/delay longer than the timeout → 408), bearer (/bearer with an Authorization header, masked by the " +
            "BearerToken preset), post (JSON body echoed back), ratelimit (a burst of calls against a sliding-window limiter → 429). " +
            "Every attempt is logged through a playground logger built with the fluent API.");
    }

    private static async Task<List<object>> RunScenario(PlaygroundHttpRequest req, string baseUrl, Serilog.ILogger logger,
                                                        List<object> attempts, CancellationToken ct) {
        var sw = Stopwatch.StartNew();
        var events = new HttpRequestEvents();

        // HttpHelper callback: one log event per attempt (retries included), as the request/response pair.
        events.Add((request, response, attempt, rateLimitWait) => {
            var status = (int)response.StatusCode;
            var level = response.IsSuccessStatusCode ? LogEventLevel.Information : LogEventLevel.Warning;
            var log = logger.ForContext("RateLimitWait", rateLimitWait);
            if (request.Headers.Authorization is { } auth)
                log = log.ForContext("Authorization", auth.ToString());   // masked by the BearerToken preset
            log.Write(level, "HttpHelper {Method} {Url} -> {StatusCode} (attempt {Attempt})",
                         request.Method.Method, request.RequestUri?.ToString(), status, attempt + 1);
            lock (attempts)
                attempts.Add(new { attempt = attempt + 1, status, url = request.RequestUri?.PathAndQuery, atMs = sw.ElapsedMilliseconds });
            return Task.CompletedTask;
        });

        var handler = new HttpClientHandlerLogging(events) { InnerHandler = Sockets };
        using var client = new HttpClient(handler, disposeHandler: false);   // keep the shared pool alive

        var rateLimit = req.Scenario == "ratelimit"
            ? new httpClientRateLimitOptions {
                IsEnabled = true, PermitLimit = Math.Max(1, req.PermitLimit), QueueLimit = 0,
                Window = TimeSpan.FromSeconds(10), SegmentsPerWindow = 1, AutoReplenishment = true
            }
            : null;

        IhttpsClientHelper helper = new httpsClientHelper(client, events, rateLimit!)
            .addTimeout(TimeSpan.FromSeconds(Math.Clamp(req.TimeoutSeconds, 1, 60)));
        if (req.Retries > 0 && req.Scenario != "ratelimit")
            helper.addRetryCondition(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout,
                                     Math.Clamp(req.Retries, 1, 5), Math.Clamp(req.Backoff, 0.1, 2));

        var bearer = string.IsNullOrWhiteSpace(req.BearerToken) ? "eyJhbGciOiJIUzI1NiJ9.demo.token" : req.BearerToken;
        var (method, path, body, headers) = req.Scenario switch {
            "success" => new Call(HttpMethod.Get, "/get"),
            "flaky"   => new Call(HttpMethod.Get, "/status/500,502,503,200"),
            "down"    => new Call(HttpMethod.Get, "/status/503"),
            // httpbin caps /delay at 10 s: keep the delay above the timeout (timeouts of 8 s or more cannot time out here).
            "timeout" => new Call(HttpMethod.Get, $"/delay/{Math.Min(Math.Clamp(req.TimeoutSeconds, 1, 60) + 2, 10)}"),
            "bearer"  => new Call(HttpMethod.Get, "/bearer", Headers: new() { ["Authorization"] = $"Bearer {bearer}" }),
            // JsonContentBuilder sends body.ToString(): pass already-serialized JSON (see HttpHelper README).
            "post"    => new Call(HttpMethod.Post, "/post", System.Text.Json.JsonSerializer.Serialize(new { orderId = "ORD-99821", email = "alice@example.com", amount = 129.99 })),
            _         => new Call(HttpMethod.Get, "/get")   // ratelimit
        };

        var count = req.Scenario == "ratelimit" ? Math.Clamp(req.Burst, 1, 20) : 1;
        var tasks = Enumerable.Range(0, count).Select(async _ => {
            var started = sw.ElapsedMilliseconds;
            using var res = await helper.SendAsync(baseUrl + path, method,
                body is null ? new NoBodyContentBuilder() : new JsonContentBuilder(), body!, headers, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            var status = (int)res.StatusCode;

            // Responses produced by HttpHelper itself (408 timeout, 429 rate limit) never reach the wire,
            // so the per-attempt callback does not see them: log the final outcome of every call.
            logger.Write(res.IsSuccessStatusCode ? LogEventLevel.Information : LogEventLevel.Error,
                         "HttpHelper call {Url} finished with {StatusCode} {Reason} in {ElapsedMs} ms",
                         baseUrl + path, status, res.ReasonPhrase, sw.ElapsedMilliseconds - started);

            return (object)new {
                status, reason = res.ReasonPhrase, elapsedMs = sw.ElapsedMilliseconds - started,
                body = text.Length > 600 ? text[..600] + "…" : text
            };
        });
        return [.. await Task.WhenAll(tasks)];
    }
}
