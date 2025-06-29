using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using System.Diagnostics;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.Telemetry;
/// <summary>
/// Interface responsible for creating TraceEntry instances from Activity objects.
/// </summary>
public interface ILoggerTelemetryTraceEntryFactory {
    /// <summary>
    /// Creates a TraceEntry based on the provided Activity span.
    /// </summary>
    /// <param name="activity">The current Activity span being processed.</param>
    /// <returns>A TraceEntry containing all relevant span metadata.</returns>
    TraceEntry Create(Activity activity);
}
/// <summary>
/// Default implementation of ILoggerTelemetryTraceEntryFactory that maps an Activity span
/// to a TraceEntry model for storage and correlation.
/// </summary>
public class LoggerTelemetryTraceEntryFactory : ILoggerTelemetryTraceEntryFactory {
    /// <summary>
    /// Converts an Activity instance into a TraceEntry, extracting timing,
    /// identifiers, tags, and any logged events.
    /// </summary>
    /// <param name="activity">The span to convert.</param>
    /// <returns>A TraceEntry object ready for persistence.</returns>
    public TraceEntry Create(Activity activity) {
        var start = activity.StartTimeUtc;
        var end = start + activity.Duration;

        var tags = activity.Tags.ToDictionary(t => t.Key, t => (object)t.Value);
        tags["trace_id"] = activity.TraceId.ToString();

        if (activity.Events.Any()) {
            var logs = activity.Events.Select(e => new {
                e.Name,
                e.Timestamp,
                Tags = e.Tags.ToDictionary(tag => tag.Key, tag => (object)tag.Value)
            }).ToList();

            tags["otel.logs"] = logs;
        }
        return new TraceEntry {
            TraceId = activity.TraceId.ToString(),
            SpanId = activity.SpanId.ToString(),
            ParentSpanId = activity.ParentSpanId.ToString(),
            Name = activity.DisplayName,
            StartTime = start,
            EndTime = end,
            DurationMs = activity.Duration.TotalMilliseconds,
            TagsJson = JsonSerializer.Serialize(tags)
        };
    }
}