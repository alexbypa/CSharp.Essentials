using CSharpEssentials.LoggerHelper.model;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public interface IMetricEntryRepository {
    Task SaveAsync(IEnumerable<MetricEntry> entries, CancellationToken token);
}
public class MetricEntryRepository : IMetricEntryRepository {
    private readonly IServiceProvider _provider;

    public MetricEntryRepository(IServiceProvider provider) {
        _provider = provider;
    }

    /// <summary>
    /// avoid to save errors in loop
    /// </summary>
    protected static bool justsavedError = false;
    public async Task SaveAsync(IEnumerable<MetricEntry> entries, CancellationToken token) {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();
        try {
            await db.Metrics.AddRangeAsync(entries, token);
            await db.SaveChangesAsync(token);
        } catch (Exception ex) {
            if (!justsavedError) {
                GlobalLogger.Errors.Add(new LogErrorEntry {
                    ContextInfo = "MetricEntryRepository.SaveAsync",
                    ErrorMessage = ex.Message + (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""),
                    StackTrace = ex.StackTrace,
                    SinkName = "Telemetry",
                    Timestamp = DateTime.Now
                });
                justsavedError = true;
            }
            Console.WriteLine($"Error saving metrics: {ex.Message}");
        }
    }
}
