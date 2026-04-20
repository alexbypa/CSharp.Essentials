using CSharpEssentials.LoggerHelper.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 5: Diagnostics — errori interni, sink caricati, health check.
/// Utile per verificare che il pipeline è configurato correttamente.
/// </summary>
public class DiagnosticsEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/diagnostics").WithTags("Diagnostics");

        // GET /api/diagnostics/health — health check del pipeline di logging
        group.MapGet("/health", (ILogErrorStore errorStore, LoggerHelperOptions options) => {
            var hasErrors = errorStore.Count > 0;
            return Results.Ok(new {
                status = hasErrors ? "degraded" : "healthy",
                applicationName = options.ApplicationName,
                routeCount = options.Routes.Count,
                sinks = options.Routes.Select(r => r.Sink),
                internalErrors = errorStore.Count,
                recentErrors = errorStore.GetAll().TakeLast(5).Select(e => new {
                    e.SinkName,
                    e.ErrorMessage,
                    e.Timestamp
                })
            });
        });

        // DELETE /api/diagnostics/errors — pulisce gli errori interni
        group.MapDelete("/errors", (ILogErrorStore errorStore) => {
            errorStore.Clear();
            return Results.Ok(new { message = "Internal error store cleared" });
        });
    }
}
