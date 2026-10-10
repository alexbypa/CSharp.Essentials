using CSharpEssentials.HttpHelper;
using CSharpEssentials.HttpHelper.HttpMocks;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.Extensions.Options;
using Serilog.Events;
using System.Diagnostics;
using System.Net;
using System.Text;
using static CSharpEssentials.LoggerHelper.Demo.Endpoints.PlaygroundEndpoints;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 12: HttpHelper nel Playground — chiamate VERE a un'API pubblica gratuita (httpbin.org di default,
/// configurabile con "Playground:HttpBaseUrl", es. un httpbin in Docker per lavorare offline).
/// Ogni scenario configura un httpsClientHelper al volo (retry Polly, timeout, rate limit, Bearer) e logga ogni
/// tentativo con il logger fluente del Playground: stessi sink, stesso masking scelti nella pagina.
/// </summary>
public class PlaygroundHttpEndpoints : IEndpointDefinition {
    public static readonly string[] Scenarios = ["success", "flaky", "down", "timeout", "bearer", "post", "ratelimit", "order"];

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

    // ponytail: process-wide switch shared by all visitors; per-session state if the demo goes multi-user
    private static volatile bool _httpLogOn = true;

    /// <summary>Wraps the host options: with the switch off LogRequests is empty (no per-attempt events) but the correlation header is still sent.</summary>
    private sealed class SwitchableLogging(IOptionsMonitor<HttpHelperLoggingOptions> host) : IOptionsMonitor<HttpHelperLoggingOptions> {
        public HttpHelperLoggingOptions CurrentValue => _httpLogOn
            ? host.CurrentValue
            : new() { LogRequests = [], CorrelationIdHeader = host.CurrentValue.CorrelationIdHeader };
        public HttpHelperLoggingOptions Get(string? _) => CurrentValue;
        public IDisposable? OnChange(Action<HttpHelperLoggingOptions, string?> listener) => host.OnChange(listener);
    }

    public sealed record LoggingSwitch(bool Enabled);

    private sealed record Call(HttpMethod Method, string Path, object? Body = null, Dictionary<string, string>? Headers = null);

    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/playground").WithTags("Playground");

        group.MapGet("/http/logging", () => Results.Ok(new { enabled = _httpLogOn }))
             .WithSummary("Playground HTTP log switch: is HttpHelperLogging:LogRequests applied?");
        group.MapPost("/http/logging", (LoggingSwitch body) => {
            _httpLogOn = body.Enabled;
            return Results.Ok(new { enabled = _httpLogOn });
        }).WithSummary("Turn HttpHelper's per-attempt request events on or off for every Playground HTTP scenario");

        group.MapPost("/http", async (PlaygroundHttpRequest req, IConfiguration config, LoggerHelperOptions hostOptions,
                                      ILogErrorStore hostErrors, IOptionsMonitor<HttpHelperLoggingOptions> httpLogging,
                                      CancellationToken ct) => {
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
                var cid = (string?)null;
                try {
                    var switchable = new SwitchableLogging(httpLogging);
                    if (req.Scenario == "order") {
                        cid = "order-" + Guid.NewGuid().ToString("N")[..8];
                        calls = await RunOrderFlow(cid, session.Logger, switchable, attempts, ct);
                    } else
                        calls = await RunScenario(req, baseUrl, session.Logger, switchable, attempts, ct);
                } finally {
                    session.Dispose();
                }
                return Results.Ok(new { scenario = req.Scenario, calls, attempts, correlationId = cid, httpLog = _httpLogOn, log = session.Report() });
            } finally {
                Gate.Release();
            }
        })
        .WithSummary("Call a free public API through HttpHelper and show its built-in request events (with CorrelationId) on the chosen sinks")
        .WithDescription(
            "order runs against an in-memory shop (login → order → payment: timeout, 503, 200) with one CorrelationId, no network. The others hit httpbin.org: success (GET /get), flaky (random 500/502/503/200), down (always 503), " +
            "timeout (/delay longer than the timeout → 408), bearer (/bearer with an Authorization header, masked by the " +
            "BearerToken preset), post (JSON body echoed back), ratelimit (a burst of calls against a sliding-window limiter → 429). " +
            "Every attempt is logged by HttpHelper's built-in request events (status, elapsed, attempt, CorrelationId) through a playground logger built with the fluent API.");
    }

    private static async Task<List<object>> RunOrderFlow(string cid, Serilog.ILogger logger, IOptionsMonitor<HttpHelperLoggingOptions> httpLogging,
                                                         List<object> attempts, CancellationToken ct) {
        const string host = "https://shop.playground.demo";
        var sw = Stopwatch.StartNew();
        var events = new HttpRequestEvents();
        events.Add((request, response, attempt, rateLimitWait) => {
            lock (attempts)
                attempts.Add(new { attempt = attempt + 1, status = (int)response.StatusCode, url = request.RequestUri?.PathAndQuery, atMs = sw.ElapsedMilliseconds });
            return Task.CompletedTask;
        });

        static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Reply(HttpStatusCode code, string json = "{}") =>
            (_, _) => Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        static bool Post(HttpRequestMessage r, string path) => r.Method == HttpMethod.Post && r.RequestUri?.AbsolutePath == path;

        var mock = new HttpMockEngine([
            new HttpMockScenario(r => Post(r, "/login"), [Reply(HttpStatusCode.OK, """{"token":"demo"}""")]),
            new HttpMockScenario(r => Post(r, "/orders"), [Reply(HttpStatusCode.Created, """{"orderId":"ORD-1001"}""")]),
            new HttpMockScenario(r => Post(r, "/payments"), [
                async (_, token) => { await Task.Delay(TimeSpan.FromSeconds(5), token); return new HttpResponseMessage(HttpStatusCode.OK); },   // honours the 500 ms timeout
                Reply(HttpStatusCode.ServiceUnavailable),
                Reply(HttpStatusCode.OK, """{"paymentId":"PAY-1","status":"captured"}""")])
        ]);

        var handler = new HttpClientHandlerLogging(events, null, "playground", httpLogging, logger) { InnerHandler = mock.Build() };
        using var client = new HttpClient(handler);
        IhttpsClientHelper helper = new httpsClientHelper(client, events, null!)
            .addTimeout(TimeSpan.FromMilliseconds(500))
            .addRetryCondition(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout, 3, 0.5);

        var header = httpLogging.CurrentValue.CorrelationIdHeader ?? "X-Correlation-ID";
        var calls = new List<object>();
        foreach (var (step, path) in new[] { ("login", "/login"), ("order", "/orders"), ("payment", "/payments") }) {
            var started = sw.ElapsedMilliseconds;
            using var res = await helper.SendAsync(host + path, HttpMethod.Post, new JsonContentBuilder(), "{}", new Dictionary<string, string> { [header] = cid }, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            var elapsed = sw.ElapsedMilliseconds - started;
            logger.ForContext("CorrelationId", cid).Write(res.IsSuccessStatusCode ? LogEventLevel.Information : LogEventLevel.Error,
                "Order step {Step} finished with {StatusCode} in {ElapsedMs} ms", step, (int)res.StatusCode, elapsed);
            calls.Add(new { step, status = (int)res.StatusCode, reason = res.ReasonPhrase, elapsedMs = elapsed, body = text });
        }
        return calls;
    }

    private static async Task<List<object>> RunScenario(PlaygroundHttpRequest req, string baseUrl, Serilog.ILogger logger,
                                                        IOptionsMonitor<HttpHelperLoggingOptions> httpLogging, List<object> attempts, CancellationToken ct) {
        var sw = Stopwatch.StartNew();
        var events = new HttpRequestEvents();

        // Per-attempt log lines come from HttpHelper's built-in events (LogRequests contains "playground");
        // the callback only feeds the UI timeline and, for bearer, logs the Authorization header (masking demo).
        events.Add((request, response, attempt, rateLimitWait) => {
            if (req.Scenario == "bearer" && request.Headers.Authorization is { } auth)
                logger.ForContext("Authorization", auth.ToString()).Information("HttpHelper bearer request carried an Authorization header");   // masked by the BearerToken preset
            lock (attempts)
                attempts.Add(new { attempt = attempt + 1, status = (int)response.StatusCode, url = request.RequestUri?.PathAndQuery, atMs = sw.ElapsedMilliseconds });
            return Task.CompletedTask;
        });

        var handler = new HttpClientHandlerLogging(events, null, "playground", httpLogging, logger) { InnerHandler = Sockets };
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
