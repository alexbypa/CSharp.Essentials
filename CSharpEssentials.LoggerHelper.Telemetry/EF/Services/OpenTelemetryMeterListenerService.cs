using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.Metrics;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            //TODO: per adesso tracciamo tutti 
            //if (instrument.Meter.Name.StartsWith("Microsoft.AspNetCore") ||
            //    instrument.Meter.Name.StartsWith("System.Net.Http")) {
            listener.EnableMeasurementEvents(instrument);
            //}
        };

        //listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) => saveMeter(instrument, measurement, tags, _));
        //listener.SetMeasurementEventCallback<float>((instrument, measurement, tags, _) => saveMeter(instrument, measurement, tags, _));
        //listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) => saveMeter(instrument, measurement, tags, _));
        //listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) => saveMeter(instrument, measurement, tags, _));
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
    private void saveMeter<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) {
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
                Value = Convert.ToDouble(measurement),
                Timestamp = DateTime.UtcNow,
                TagsJson = JsonSerializer.Serialize(tagDict),
                TraceId = traceId
            });
            try {
                await db.SaveChangesAsync();

            } catch (DbUpdateException dbEx) {
                if (dbEx.InnerException is PostgresException pg)
                    Debug.WriteLine($"Postgres error: {pg.MessageText}");
                else
                    Debug.WriteLine($"EF error: {dbEx.Message}");
            } catch (Exception ex) {
                Debug.WriteLine($"Error saving metric: {ex.Message} - {ex.StackTrace}");
            }
        
        //} catch (Exception ex) {
        //    Debug.WriteLine($"Error saving metric: {ex.Message} - {ex.StackTrace}");
        //}
    
            });
    }
}
