using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using System.Diagnostics;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public interface ITraceEntryFactory {
    TraceEntry Create(Activity activity);
}

public class TraceEntryFactory : ITraceEntryFactory {
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