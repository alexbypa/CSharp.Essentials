using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.Telemetry;

public class PostgreSqlMetricExporter : BaseExporter<Metric> {
    private readonly IServiceProvider _provider;

    public PostgreSqlMetricExporter(IServiceProvider provider) {
        _provider = provider;
    }

    public override ExportResult Export(in Batch<Metric> batch) {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();

        foreach (var metric in batch) {
            foreach (ref readonly var metricPoint in metric.GetMetricPoints()) {
                // Semplificazione: usiamo solo valore "double"
                var value = metricPoint.GetSumDouble();

                var tags = new Dictionary<string, object>();
                foreach (var tag in metricPoint.Tags)
                    tags[tag.Key] = tag.Value!;

                // Extract trace_id se presente
                string? traceId = null;
                if (tags.TryGetValue("trace_id", out var traceValue))
                    traceId = traceValue?.ToString();

                var metricEntry = new MetricEntry {
                    Name = metric.Name,
                    Value = value,
                    Timestamp = DateTime.UtcNow,
                    TagsJson = JsonSerializer.Serialize(tags),
                    TraceId = traceId
                };

                db.Metrics.Add(metricEntry);
            }
        }
        db.SaveChanges();
        return ExportResult.Success;
    }
}
