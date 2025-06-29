/*
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Text.Json;
*/

namespace CSharpEssentials.LoggerHelper.Telemetry.Depreceted;

// Questo exporter è stato deprecato in favore di LoggerTelemetryMeterListenerService
// perché non riusciva a leggere il trace_id direttamente (salvo workaround complicati).

/*
/// <summary>
/// Exports OpenTelemetry metrics to a PostgreSQL database.
/// Iterates over each MetricPoint in the batch, extracts the value and tags,
/// then creates and saves a MetricEntry record in EF Core.
/// </summary>
public class PostgreSqlMetricExporter : BaseExporter<Metric> {
    private readonly IServiceProvider _provider;
    /// <summary>
    /// Initializes a new instance of PostgreSqlMetricExporter.
    /// </summary>
    /// <param name="provider">Service provider to resolve the EF DbContext scope for saving metrics.</param>
    public PostgreSqlMetricExporter(IServiceProvider provider) {
        _provider = provider;
    }
    /// <summary>
    /// Export a batch of metrics to PostgreSQL.
    /// </summary>
    /// <param name="batch">The batch of metrics provided by OpenTelemetry.
    /// Each Metric contains multiple MetricPoints to persist.</param>
    /// <returns>ExportResult.Success if all metrics were handled (errors are logged).</returns>
    public override ExportResult Export(in Batch<Metric> batch) {
        // Create a new DI scope to retrieve TelemetriesDbContext
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
                    foreach (var tag in point.Tags) {
                        Console.WriteLine($"📌 TAG: {tag.Key} = {tag.Value}");
                        tags[tag.Key] = tag.Value!;
                    }

                    //TODO: verificare che trace_id sia sempre presente
                    if (tags.TryGetValue("trace_id", out var traceValue)) {
                        Console.WriteLine($"✅ trace_id found: {traceValue}");
                    } else {
                        Console.WriteLine($"❌ NO trace_id found for metric: {metric.Name}");
                    }

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
}
*/