using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration;
/// <summary>
/// Configures the Entity Framework Core DbContext for telemetry data using the provided connection string.
/// Applies automatic database migrations on startup.
/// </summary>
public static class LoggerTelemetryDbConfigurator {
    /// <summary>
    /// Registers the TelemetriesDbContext with dependency injection and applies any pending EF Core migrations.
    /// </summary>
    /// <param name="canContinueWithTelemetry">Outputs whether telemetry can continue based on database availability.</param>
    /// <param name="services">The service collection to register the DbContext into.</param>
    public static void InitializeMigrationsAndDbContext(IServiceCollection services, IConfiguration configuration, out bool canContinueWithTelemetry) {
        canContinueWithTelemetry = true;

        var ConnectionString = configuration.GetValue<string>("Serilog:SerilogConfiguration:LoggerTelemetryOptions:ConnectionString");
        var provider = configuration.GetValue<string>("Serilog:SerilogConfiguration:LoggerTelemetryOptions:Provider");

        //To load connection string fromdocker environment variable if exists
        //TOHACK:
        /*
        var options = services.BuildServiceProvider()
                      .GetRequiredService<IOptions<LoggerTelemetryOptions>>()
                      .Value;
        var provider = options.Provider;
        */

        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)) {
            services.AddDbContext<TelemetriesDbContext, TelemetryDbContextNpgsql>(opt =>
                opt.UseNpgsql(ConnectionString, b =>
                    b.MigrationsAssembly(typeof(TelemetryDbContextNpgsql).Assembly.FullName))
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .LogTo(Console.WriteLine, LogLevel.Information)
                );
        } else {
            services.AddDbContext<TelemetriesDbContext, TelemetryDbContextSqlServer>(opt =>
                opt.UseSqlServer(ConnectionString, b =>
                    b.MigrationsAssembly(typeof(TelemetryDbContextSqlServer).Assembly.FullName))
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .LogTo(Console.WriteLine, LogLevel.Information)
                );
        }
        /*
        services.AddDbContext<TelemetriesDbContext>(cfg =>
            cfg.UseNpgsql(options.ConnectionString)
               .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
        */

        using (var scope = services.BuildServiceProvider().CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();

            Console.WriteLine($"Context: {db.GetType().FullName}");
            Console.WriteLine($"Provider: {db.Database.ProviderName}");
            Console.WriteLine("Migrations assembly: " +
                db.GetService<IMigrationsAssembly>().Assembly.GetName().Name);

            var all = db.Database.GetMigrations().ToArray();
            var pending = db.Database.GetPendingMigrations().ToArray();
            Console.WriteLine($"All migrations: {string.Join(", ", all)}");
            Console.WriteLine($"Pending: {string.Join(", ", pending)}");

            var hasMigrations = db.Database.GetMigrations().Any();
            if (hasMigrations) {
                // flusso EF standard
                db.Database.Migrate();
                return;
            }
            //#if DEBUG
            db.Database.EnsureCreated(); // crea tutte le tabelle dal modello

            string tableName = "LogEntry";
            try {
                if (db.LogEntry.FirstOrDefault() != null)
                    Console.WriteLine("Table LogEntry founded");
                tableName = "TraceEntry";
                if (db.TraceEntry.FirstOrDefault() != null)
                    Console.WriteLine("Table TraceEntry founded");
                tableName = "MetricEntry";
                if (db.Metrics.FirstOrDefault() != null)
                    Console.WriteLine("Table MetricEntry founded");
            } catch (Exception ex) {
                GlobalLogger.Errors.Add(new model.LogErrorEntry {
                    ContextInfo = "LoggerTelemetryDbConfigurator.Configure",
                    ErrorMessage = $"Table {tableName} not found or Error on layout : Run dotnet ef database update --context <YourDbContext> --startup-project <PathYourWebApi>\r\n. Error: {ex.Message}",
                    SinkName = "Migration EF",
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow
                });
                canContinueWithTelemetry = false;
            }
            /*
                        var creator = db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
                        var createSql = creator.GenerateCreateScript();
                        db.Database.ExecuteSqlRaw(createSql);
            */
            return;

            //#endif

            //db.Database.Migrate();
        }
    }
}
