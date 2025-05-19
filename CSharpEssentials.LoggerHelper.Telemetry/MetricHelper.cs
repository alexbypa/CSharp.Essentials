using System.Diagnostics.Metrics;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;

namespace CSharpEssentials.LoggerHelper.Telemetry;

public static class MetricHelper {
    private static readonly Meter Meter = new("LoggerHelper.Metrics");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("http.server.duration");

    public static void RecordRequestDuration(double milliseconds, string route, string method) {
        var traceId = Baggage.GetBaggage("trace_id") ?? "unknown";

        RequestDuration.Record(milliseconds,
            new("trace_id", traceId),
            new("http.route", route),
            new("http.method", method)
        );
    }
}
