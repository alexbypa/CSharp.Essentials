using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public interface IMetricEntryFactory {
    MetricEntry Create(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags);
}
public class MetricEntryFactory : IMetricEntryFactory {
    public MetricEntry Create(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags) {
        var dict = new Dictionary<string, object>();
        foreach (var tag in tags)
            dict[tag.Key] = tag.Value!;

        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId))
            dict["trace_id"] = traceId;

        return new MetricEntry {
            Name = instrument.Name,
            Value = value,
            Timestamp = DateTime.UtcNow,
            TagsJson = JsonSerializer.Serialize(dict),
            TraceId = traceId
        };
    }
}