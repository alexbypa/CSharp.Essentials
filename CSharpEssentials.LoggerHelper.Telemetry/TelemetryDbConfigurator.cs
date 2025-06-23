using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public static class TelemetryDbConfigurator {
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
