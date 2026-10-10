using CSharpEssentials.HttpHelper;
using CSharpEssentials.HttpHelper.HttpMocks;
using System.Net;
using System.Text;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Endpoint module — HttpHelper demo.
///
/// GET /api/httphelper/retry calls an in-memory flaky upstream (502, 503, then 200) through
/// HttpHelper with a retry policy; every attempt is logged and visible live in /loggerhelper.
/// </summary>
public class HttpHelperEndpoints : IEndpointDefinition {
    public const string ClientName = "demo-flaky";

    /// <summary>In-memory upstream answering 502, 503, 200 (round-robin). No real network involved.</summary>
    public static IHttpMockScenario FlakyUpstream { get; } = new HttpMockScenario(
        match: r => r.RequestUri?.Host == "flaky.httphelper.demo",
        responseFactory: [
            () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)),
            () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("""{"orders":[{"id":1}]}""", Encoding.UTF8, "application/json")
            })
        ]);

    public void DefineEndpoints(WebApplication app) {
        var factory = app.Services.GetRequiredService<IhttpsClientHelperFactory>();

        // registered once: per-request registration would leak callbacks.
        factory.CreateOrGet(ClientName).addRetryCondition(r => (int)r.StatusCode >= 500, retryCount: 3, backoffFactor: 1);
        // per-attempt log: built-in, enabled by "HttpHelperLogging" in appsettings.json (no callback needed).

        var group = app.MapGroup("/api/httphelper").WithTags("HttpHelper");

        group.MapGet("/retry", async (CancellationToken ct) => {
            using var res = await factory.CreateOrGet(ClientName)
                .SendAsync("https://flaky.httphelper.demo/orders", HttpMethod.Get, cancellationToken: ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            return Results.Ok(new { status = (int)res.StatusCode, body, dashboard = "/loggerhelper" });
        })
        .WithSummary("HttpHelper retry — flaky upstream (502, 503, 200)")
        .WithDescription(
            "Calls an in-memory flaky upstream through HttpHelper with retry on 5xx. " +
            "Hit it, then open /loggerhelper: two Error attempts (502, 503) + one Information.");
    }
}
