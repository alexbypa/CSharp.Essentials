using Microsoft.AspNetCore.Http;
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
    public TraceIdPropagationMiddleware(RequestDelegate next) {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }
    public async Task InvokeAsync(HttpContext context) {
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
        await _next(context);
    }
}
