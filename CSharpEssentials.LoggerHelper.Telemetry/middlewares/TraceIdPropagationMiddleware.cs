using CSharpEssentials.LoggerHelper.Configuration;
using CSharpEssentials.LoggerHelper.Telemetry.Proxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Telemetry.middlewares;
/// <summary>
/// Middleware to propagate the current Activity's TraceId into
/// both the Activity.Tags and the OpenTelemetry Baggage, ensuring
/// all downstream telemetry (metrics and traces) include the trace_id tag.
/// </summary>
public class TraceIdPropagationMiddleware {
    private readonly RequestDelegate _next;
    /// <summary>
    /// Initializes a new instance of the <see cref="TraceIdPropagationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware component in the pipeline.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="next"/> is null.</exception>
    public TraceIdPropagationMiddleware(RequestDelegate next) {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }
    /// <summary>
    /// Middleware execution logic that extracts the current Activity TraceId, 
    /// adds it to both the Activity's tags and the OpenTelemetry Baggage, 
    /// and calls the next middleware in the pipeline.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context) {
        var config = context.RequestServices.GetService<ITelemetryGatekeeper>();
        if (config?.IsEnabled ?? false) {
            var activity = Activity.Current;
            if (activity is not null) {
                // Extract the TraceId
                var traceId = activity.TraceId.ToString();
                // Add to Activity tags if not already present
                if (!activity.Tags.Any(t => t.Key == "trace_id")) {
                    activity.SetTag("trace_id", traceId);
                }
                // Add to Baggage for propagation into metrics
                if (string.IsNullOrEmpty(Baggage.GetBaggage("trace_id"))) {
                    Baggage.SetBaggage("trace_id", traceId);
                }
            }
            // Call the next middleware in the pipeline
        }
        await _next(context);
    }
}
