using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.Metrics;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using System.Text.Json;

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

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) => {
            var tagArray = tags.ToArray();

            _ = Task.Run(async () => {
                using var scope = _provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                var tagDict = new Dictionary<string, object>();
                foreach (var tag in tagArray) {
                    tagDict[tag.Key] = tag.Value!;
                }

                await db.Metrics.AddAsync(new MetricEntry {
                    Name = instrument.Name,
                    Value = measurement,
                    Timestamp = DateTime.UtcNow,
                    TagsJson = JsonSerializer.Serialize(tagDict)
                });

                await db.SaveChangesAsync();
            });
        });



        listener.Start();

        return Task.CompletedTask;
    }
}
