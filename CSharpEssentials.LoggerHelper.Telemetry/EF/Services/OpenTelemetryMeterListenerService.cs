using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.Metrics;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using System.Text.Json;
using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Services;
public class OpenTelemetryMeterListenerService : BackgroundService {
    private readonly IServiceProvider _provider;

    public OpenTelemetryMeterListenerService(IServiceProvider provider) {
        _provider = provider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        var listener = new MeterListener();

        listener.InstrumentPublished = (instrument, listener) => {
            // Prende TUTTE le metriche, ma puoi filtrare per nome o origine
            if (instrument.Meter.Name.StartsWith("Microsoft.AspNetCore") ||
                instrument.Meter.Name.StartsWith("System.Net.Http")) {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            var tagArray = tags.ToArray();
            var traceId = Activity.Current?.TraceId.ToString();

            _ = Task.Run(async () =>
            {
                using var scope = _provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();

                var tagDict = new Dictionary<string, object>();
                foreach (var tag in tagArray)
                    tagDict[tag.Key] = tag.Value!;

                // ✅ Includi il traceId anche nei tags se utile
                if (!string.IsNullOrWhiteSpace(traceId))
                    tagDict["trace_id"] = traceId;

                await db.Metrics.AddAsync(new MetricEntry {
                    Name = instrument.Name,
                    Value = measurement,
                    Timestamp = DateTime.UtcNow,
                    TagsJson = JsonSerializer.Serialize(tagDict),
                    TraceId = traceId
                });

                await db.SaveChangesAsync();
            });
        });




        listener.Start();

        return Task.CompletedTask;
    }
}
