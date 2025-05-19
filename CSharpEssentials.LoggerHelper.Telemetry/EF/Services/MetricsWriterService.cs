using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Services;
public class MetricsWriterService : BackgroundService {
    private readonly IServiceProvider _provider;

    public MetricsWriterService(IServiceProvider provider) {
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();

            var now = DateTime.UtcNow;

            //db.Metrics.Add(new MetricEntry { Name = "current_second", Value = DateTime.UtcNow.Second, Timestamp = now });
            db.Metrics.AddRange(new[]
        {
            new MetricEntry { Name = "current_second", Value = CustomMetrics.CurrentSecondGauge.LastValue, Timestamp = now },
            new MetricEntry { Name = "memory_used_mb", Value = CustomMetrics.MemoryUsedGauge.LastValue, Timestamp = now },
            new MetricEntry { Name = "postgresql.connections.active", Value = CustomMetrics.ActivePostgresConnectionsGauge.LastValue, Timestamp = now }
        });

            await db.SaveChangesAsync(stoppingToken);

            await Task.Delay(10000, stoppingToken);
        }
    }
}
