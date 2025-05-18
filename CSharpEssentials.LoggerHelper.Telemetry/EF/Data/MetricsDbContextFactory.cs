using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
public class MetricsDbContextFactory : IDesignTimeDbContextFactory<MetricsDbContext> {
    public MetricsDbContext CreateDbContext(string[] args) {
        IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory()) // Dove cercare il file
               .AddJsonFile("appsettings.json", optional: true)
               .Build();
        var connectionString = configuration.GetConnectionString("MetricsDb");
        //var connectionString = "Host=51.178.131.166:1433;Username=postgres;Password=PixPstG!!;Database=HubGamePragmaticCasino;Search Path=dbo,public;ConnectionLifetime=30;";

        var optionsBuilder = new DbContextOptionsBuilder<MetricsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new MetricsDbContext(optionsBuilder.Options);
    }
}