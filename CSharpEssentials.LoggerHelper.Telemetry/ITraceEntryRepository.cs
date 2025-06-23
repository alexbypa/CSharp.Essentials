using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public interface ITraceEntryRepository {
    void Save(IEnumerable<TraceEntry> entries);
}

public class TraceEntryRepository : ITraceEntryRepository {
    private readonly IServiceProvider _provider;

    public TraceEntryRepository(IServiceProvider provider) {
        _provider = provider;
    }

    public void Save(IEnumerable<TraceEntry> entries) {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();
        db.TraceEntry.AddRange(entries);
        db.SaveChanges();
    }
}
