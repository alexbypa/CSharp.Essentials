using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 4: Verifica routing per livello.
/// Dimostra che ogni sink riceve SOLO i livelli configurati nel JSON.
///
/// Con la configurazione di esempio:
///   Console  → Information, Warning, Error, Fatal
///   File     → Warning, Error, Fatal
///   MSSqlServer → Information (solo Information!)
///   PostgreSQL  → Error, Fatal
///
/// Questo endpoint logga a tutti i livelli e puoi verificare
/// in ogni sink quale messaggio è arrivato.
/// </summary>
public class RoutingDemoEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/routing").WithTags("Routing Demo");

        // GET /api/routing/test — logga a tutti i livelli per verificare il routing
        group.MapGet("/test", (ILogger<RoutingDemoEndpoints> logger) => {
            var txnId = $"ROUTE-{DateTime.UtcNow:HHmmss}";

            using (logger.BeginTrace("RoutingTest", txnId)) {
                logger.LogTrace("TRACE level — should appear in: (nothing, filtered by MinimumLevel)");
                logger.LogDebug("DEBUG level — should appear in: (nothing, filtered by MinimumLevel)");
                logger.LogInformation("INFORMATION level — should appear in: Console, MSSqlServer");
                logger.LogWarning("WARNING level — should appear in: Console, File");
                logger.LogError("ERROR level — should appear in: Console, File, PostgreSQL");
                logger.LogCritical("CRITICAL/FATAL level — should appear in: Console, File, PostgreSQL");
            }

            return Results.Ok(new {
                message = "6 log levels emitted — check each sink to verify routing",
                txnId,
                expectedRouting = new {
                    Console = new[] { "Information", "Warning", "Error", "Fatal" },
                    File = new[] { "Warning", "Error", "Fatal" },
                    MSSqlServer = new[] { "Information" },
                    PostgreSQL = new[] { "Error", "Fatal" }
                }
            });
        });

        // GET /api/routing/errors — mostra errori interni del LoggerHelper (sink down, config errate)
        group.MapGet("/errors", (ILogErrorStore errorStore) => {
            return Results.Ok(new {
                count = errorStore.Count,
                errors = errorStore.GetAll()
            });
        });

        // GET /api/routing/config — mostra la configurazione corrente
        group.MapGet("/config", (LoggerHelperOptions options) => {
            return Results.Ok(new {
                applicationName = options.ApplicationName,
                routes = options.Routes.Select(r => new {
                    sink = r.Sink,
                    levels = r.Levels
                }),
                general = new {
                    options.General.EnableSelfLogging,
                    options.General.EnableRequestResponseLogging,
                    options.General.EnableOpenTelemetry,
                    options.General.EnableRenderedMessage
                }
            });
        });
    }
}
