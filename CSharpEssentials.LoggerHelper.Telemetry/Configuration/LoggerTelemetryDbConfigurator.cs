using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration;
/// <summary>
/// Configures the Entity Framework Core DbContext for telemetry data using the provided connection string.
/// Applies automatic database migrations on startup.
/// </summary>
public static class LoggerTelemetryDbConfigurator {
    /// <summary>
    /// Registers the TelemetriesDbContext with dependency injection and applies any pending EF Core migrations.
    /// </summary>
    /// <param name="services">The service collection to register the DbContext into.</param>
    /// <param name="options">The telemetry options containing the database connection string.</param>
    public static void Configure(IServiceCollection services, LoggerTelemetryOptions options) {
        services.AddDbContext<TelemetriesDbContext>(cfg =>
            cfg.UseNpgsql(options.ConnectionString)
               .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        using (var scope = services.BuildServiceProvider().CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();
            db.Database.Migrate();
        }
    }
}
