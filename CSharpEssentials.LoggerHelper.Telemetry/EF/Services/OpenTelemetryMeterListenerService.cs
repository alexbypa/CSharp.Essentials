using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Diagnostics.Metrics;
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
            listener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) => {
            var tagArray = tags.ToArray();
            var traceId = Activity.Current?.TraceId.ToString();

            _ = Task.Run(async () => {
                using var scope = _provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();

                var tagDict = new Dictionary<string, object>();
                foreach (var tag in tagArray)
                    tagDict[tag.Key] = tag.Value!;

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
