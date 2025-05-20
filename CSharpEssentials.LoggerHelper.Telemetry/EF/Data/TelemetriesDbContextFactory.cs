using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
public class TelemetriesDbContextFactory : IDesignTimeDbContextFactory<TelemetriesDbContext> {
    public TelemetriesDbContext CreateDbContext(string[] args) {
        IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory()) // Dove cercare il file
               .AddJsonFile("appsettings.json", optional: true)
               .Build();
        var connectionString = configuration.GetConnectionString("MetricsDb");

        var optionsBuilder = new DbContextOptionsBuilder<TelemetriesDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new TelemetriesDbContext(optionsBuilder.Options);
    }
}