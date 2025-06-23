using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public interface IMetricEntryRepository {
    Task SaveAsync(IEnumerable<MetricEntry> entries, CancellationToken token);
}
public class MetricEntryRepository : IMetricEntryRepository {
    private readonly IServiceProvider _provider;

    public MetricEntryRepository(IServiceProvider provider) {
        _provider = provider;
    }

    public async Task SaveAsync(IEnumerable<MetricEntry> entries, CancellationToken token) {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();
        await db.Metrics.AddRangeAsync(entries, token);
        await db.SaveChangesAsync(token);
    }
}
