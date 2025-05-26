using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using System.Diagnostics;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.Telemetry;
/// <summary>
/// Exports completed OpenTelemetry Activities (spans) to a PostgreSQL database.
/// Creates TraceEntry records for each Activity with properties like TraceId, SpanId, timestamps, and tags.
/// </summary>
public class PostgreSqlTraceExporter : BaseExporter<Activity> {
    private readonly IServiceProvider _provider;
    /// <summary>
    /// Initializes a new instance of the PostgreSqlTraceExporter.
    /// </summary>
    /// <param name="provider">DI service provider to create scopes for EF context resolution.</param>
    public PostgreSqlTraceExporter(IServiceProvider provider) {
        _provider = provider;
    }
    /// <summary>
    /// Exports a batch of completed Activity instances to the database.
    /// </summary>
    /// <param name="batch">Batch of Activities to export.</param>
    /// <returns>ExportResult indicating success or failure.</returns>
    public override ExportResult Export(in Batch<Activity> batch) {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();

        foreach (var activity in batch) {
            var start = activity.StartTimeUtc;
            var end = start + activity.Duration;
            var tags = activity.Tags.ToDictionary(t => t.Key, t => (object)t.Value);

            tags["trace_id"] = activity.TraceId.ToString();

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
