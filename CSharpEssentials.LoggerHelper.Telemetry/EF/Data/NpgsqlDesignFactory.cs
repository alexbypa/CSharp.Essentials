using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
public class NpgsqlDesignFactory : IDesignTimeDbContextFactory<TelemetryDbContextNpgsql> {
    public TelemetryDbContextNpgsql CreateDbContext(string[] args) {
        var options = new DbContextOptionsBuilder<TelemetryDbContextNpgsql>()
            .UseNpgsql("Host=localhost;Database=telemetry;Username=postgres;Password=postgres")
            .Options;
        return new TelemetryDbContextNpgsql(options);
    }
}