using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Diagnostics;
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
            foreach (ref readonly var point in metric.GetMetricPoints()) {
                double value;
                try {
                    switch (metric.MetricType) {
                        case MetricType.DoubleSum:      // contatori cumulativi double
                            value = point.GetSumDouble();
                            break;
                        case MetricType.LongSum:        // contatori cumulativi long
                            value = point.GetSumLong();
                            break;
                        case MetricType.Histogram:      // istogrammi
                            value = point.GetHistogramSum();
                            break;
                        case MetricType.DoubleGauge:     // gauge (osservabili) con valore double
                            value = point.GetGaugeLastValueDouble();
                            break;
                        case MetricType.ExponentialHistogram: // istogrammi esponenziali
                            value = point.GetExponentialHistogramData().Scale;
                            break;
                        case MetricType.LongGauge:      // gauge (osservabili) con valore long
                            value = point.GetGaugeLastValueLong();
                            break;
                        case MetricType.DoubleSumNonMonotonic: // contatori a somma doppia (up-down)
                            value = point.GetSumDouble();
                            break;
                        case MetricType.LongSumNonMonotonic:   // contatori a somma long (up-down)
                            value = point.GetSumLong();
                            break;
                        default:
                            // altri tipi (es. Summary, Leasure) li saltiamo
                            continue;
                    }

                    // raccolta tag
                    var tags = new Dictionary<string, object>();
                    foreach (var tag in point.Tags)
                        tags[tag.Key] = tag.Value!;

                    tags.TryGetValue("trace_id", out var traceValue);

                    // popoliamo l'entità EF
                    var entry = new MetricEntry {
                        Name = metric.Name,
                        Value = value,
                        Timestamp = DateTime.UtcNow,
                        TagsJson = JsonSerializer.Serialize(tags),
                        TraceId = traceValue?.ToString()
                    };

                    db.Metrics.Add(entry);
                } catch (Exception ex) {
                    Debug.WriteLine($"Errore durante l'elaborazione della metrica {metric.Name}: {ex.Message}");
                }
            }
        }
        try {
            db.SaveChanges();
        } catch (Exception ex) {
            Debug.WriteLine($"Errore durante l'elaborazione della metrica {ex}");
        }
        return ExportResult.Success;
    }
    public ExportResult Export_Withbug(in Batch<Metric> batch) {
        try {
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
        } catch (Exception ex) {
            Debug.Print(ex.ToString());
        }
        return ExportResult.Success;
    }
}
