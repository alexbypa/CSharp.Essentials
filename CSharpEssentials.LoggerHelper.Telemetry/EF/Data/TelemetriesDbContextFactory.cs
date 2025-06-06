using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
/// <summary>
/// Design-time factory for creating instances of <see cref="TelemetriesDbContext"/>.
/// This factory is used by EF Core tools (e.g., migrations) to obtain a <see cref="TelemetriesDbContext"/>
/// when running commands from the command line or Package Manager Console.
/// </summary>
public class TelemetriesDbContextFactory : IDesignTimeDbContextFactory<TelemetriesDbContext> {
    /// <summary>
    /// Creates a new <see cref="TelemetriesDbContext"/> using configuration settings
    /// (e.g., connection string) loaded from a JSON file.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments passed by EF Core tools. Not used in this implementation.
    /// </param>
    /// <returns>
    /// A <see cref="TelemetriesDbContext"/> configured to connect to the PostgreSQL database
    /// specified in configuration.
    /// </returns>
    public TelemetriesDbContext CreateDbContext(string[] args) {
        IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory()) // Dove cercare il file
               .AddJsonFile("appsettings.LoggerHelper.debug.json", optional: true)
               .Build();

        var connectionString = configuration.GetValue<string>("Serilog:SerilogConfiguration:LoggerTelemetryOptions:ConnectionString");

        var optionsBuilder = new DbContextOptionsBuilder<TelemetriesDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new TelemetriesDbContext(optionsBuilder.Options);
    }
}