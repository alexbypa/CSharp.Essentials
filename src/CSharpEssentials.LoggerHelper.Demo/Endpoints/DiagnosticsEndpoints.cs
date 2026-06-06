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
        group.MapGet("/health", (ILogErrorStore errorStore, ILoadedSinkStore loadedSinks, LoggerHelperOptions options) => {
            var hasErrors = errorStore.Count > 0;
            return Results.Ok(new {
                status = hasErrors ? "degraded" : "healthy",
                applicationName = options.ApplicationName,
                routeCount = options.Routes.Count,
                sinks = options.Routes.Select(r => r.Sink),
                loadedSinks = loadedSinks.GetAll().Select(s => new {
                    s.SinkName,
                    s.PluginType,
                    levels = s.Levels
                }),
                internalErrors = errorStore.Count,
                recentErrors = errorStore.GetAll().TakeLast(5).Select(e => new {
                    e.SinkName,
                    e.ErrorMessage,
                    e.Timestamp
                })
            });
        })
        .WithSummary("Pipeline health check")
        .WithDescription(
            "Returns 'healthy' when no internal errors are recorded, 'degraded' otherwise. " +
            "Includes the list of loaded sink plugins with their type names and configured levels, " +
            "and the last 5 internal errors if any. " +
            "Run this first after startup to confirm every sink loaded correctly before testing the other endpoints.");

        // DELETE /api/diagnostics/errors — pulisce gli errori interni
        group.MapDelete("/errors", (ILogErrorStore errorStore) => {
            errorStore.Clear();
            return Results.Ok(new { message = "Internal error store cleared" });
        })
        .WithSummary("Clear the internal error store")
        .WithDescription(
            "Empties the ILogErrorStore in-memory buffer. " +
            "Use this to reset the error count after investigating a 'degraded' health status " +
            "so you can detect new errors cleanly. The operation is idempotent.");
    }
}
