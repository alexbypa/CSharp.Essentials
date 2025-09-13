using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
public class SqlServerDesignFactory : IDesignTimeDbContextFactory<TelemetryDbContextSqlServer> {
    public TelemetryDbContextSqlServer CreateDbContext(string[] args) {
        var options = new DbContextOptionsBuilder<TelemetryDbContextSqlServer>()
            .UseSqlServer("Server=.;Database=telemetry;Trusted_Connection=True;Encrypt=False")
            .Options;
        return new TelemetryDbContextSqlServer(options);
    }
}
