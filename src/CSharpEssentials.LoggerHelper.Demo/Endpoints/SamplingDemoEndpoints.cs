namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Endpoint module 9 — Per-route log sampling demonstration.
/// Shows how SamplingRate reduces log volume on specific sinks while
/// keeping 100% of events flowing to others.
/// </summary>
public class SamplingDemoEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/sampling").WithTags("Sampling");

        group.MapGet("/burst", (ILogger<SamplingDemoEndpoints> logger, LoggerHelperOptions options) => {
            const int count = 100;
            for (int i = 0; i < count; i++) {
                logger.LogInformation("Sampled event {Index} of {Total}", i + 1, count);
            }

            var rates = options.Routes.Select(r => new {
                sink = r.Sink,
                samplingRate = r.SamplingRate ?? 1.0
            });

            return Results.Ok(new {
                message = $"Emitted {count} Information logs — check each sink to see how many arrived",
                configuredRates = rates,
                note = "A sink with SamplingRate 0.5 should receive ~50 of the 100 events"
            });
        })
        .WithSummary("Emit 100 logs — observe sampling in action")
        .WithDescription(
            "Fires 100 Information-level logs in a tight loop. " +
            "Sinks with SamplingRate < 1.0 receive only a fraction of them. " +
            "Compare the Console output count against a database sink count to verify the ratio. " +
            "The response shows each sink's configured SamplingRate for cross-reference.");

        group.MapGet("/config", (LoggerHelperOptions options) => {
            return Results.Ok(new {
                routes = options.Routes.Select(r => new {
                    sink = r.Sink,
                    levels = r.Levels,
                    samplingRate = r.SamplingRate,
                    effectiveRate = r.SamplingRate is null or >= 1.0 ? "100% (all events)" :
                                   r.SamplingRate <= 0.0 ? "0% (sink disabled)" :
                                   $"{r.SamplingRate:P0}"
                })
            });
        })
        .WithSummary("Show sampling configuration for all routes")
        .WithDescription(
            "Returns each route's SamplingRate alongside its levels. " +
            "Null or 1.0 means 100% (no sampling). " +
            "Use this to verify your JSON configuration before running /burst.");
    }
}
