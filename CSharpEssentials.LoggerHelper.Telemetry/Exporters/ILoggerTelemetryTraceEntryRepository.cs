using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using CSharpEssentials.LoggerHelper.Telemetry.Proxy;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.Telemetry.Exporters;
/// <summary>
/// Interface for saving trace entries into a persistent store.
/// </summary>
public interface ILoggerTelemetryTraceEntryRepository {

    /// <summary>
    /// Saves a collection of trace entries asynchronously.
    /// </summary>
    /// <param name="entries">The trace entries to be saved.</param>
    Task SaveAsync(IEnumerable<TraceEntry> entries);
}
/// <summary>
/// Default implementation of ILoggerTelemetryTraceEntryRepository that persists trace entries using Entity Framework Core.
/// </summary>
public class LoggerTelemetryTraceEntryRepository : ILoggerTelemetryTraceEntryRepository {
    private readonly IServiceProvider _provider;
    private readonly ITelemetryGatekeeper _telemetryGatekeeper;
    /// <summary>
    /// Constructor injecting IServiceProvider to resolve TelemetriesDbContext via scoped lifetime.
    /// </summary>
    /// <param name="provider">The application's dependency injection provider.</param>
    /// <param name="telemetryGatekeeper">Read from IOptionsMonitor is isEnebled or not</param>
    public LoggerTelemetryTraceEntryRepository(IServiceProvider provider, ITelemetryGatekeeper telemetryGatekeeper) {
        _provider = provider;
        _telemetryGatekeeper = telemetryGatekeeper;
    }
    /// <summary>
    /// Persists the given trace entries to the Telemetries database context.
    /// A scoped lifetime is used to ensure proper resource cleanup.
    /// </summary>
    /// <param name="entries">A list of trace entries to be added to the database.</param>
    public async Task SaveAsync(IEnumerable<TraceEntry> entries) {
        if (_telemetryGatekeeper.IsEnabled) {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();
            await db.TraceEntry.AddRangeAsync(entries);
            await db.SaveChangesAsync();
        }

    }
}