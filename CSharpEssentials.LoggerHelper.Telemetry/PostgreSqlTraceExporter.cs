using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using System.Diagnostics;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public class PostgreSqlTraceExporter : BaseExporter<Activity> {
    private readonly IServiceProvider _provider;

    public PostgreSqlTraceExporter(IServiceProvider provider) {
        _provider = provider;
    }
    public override ExportResult Export(in Batch<Activity> batch) {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();

        foreach (var activity in batch) {
            var start = activity.StartTimeUtc;
            var end = start + activity.Duration;
            var tags = activity.Tags.ToDictionary(t => t.Key, t => (object)t.Value);

            // Include anche eventuali log (eventi)
            if (activity.Events.Any()) {
                var logs = activity.Events.Select(e => new {
                    e.Name,
                    e.Timestamp,
                    Tags = e.Tags.ToDictionary(tag => tag.Key, tag => (object)tag.Value)
                }).ToList();

                tags["otel.logs"] = logs;
            }

            db.TraceEntry.Add(new TraceEntry {
                TraceId = activity.TraceId.ToString(),
                SpanId = activity.SpanId.ToString(),
                ParentSpanId = activity.ParentSpanId.ToString(),
                Name = activity.DisplayName,
                StartTime = start,
                EndTime = end,
                DurationMs = activity.Duration.TotalMilliseconds,
                TagsJson = JsonSerializer.Serialize(tags)
            });
        }

        try {
            db.SaveChanges();
        }catch (Exception ex) {
            Debug.WriteLine($"Error saving trace entries: {ex.ToString()}");
        }

        return ExportResult.Success;
    }
}
