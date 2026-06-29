using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 8: Contextual Error Logging — ring buffer che cattura il contesto pre-errore.
/// Quando si verifica un errore, i log di contesto (Debug/Info/Warning) vengono automaticamente
/// flushed insieme all'errore, fornendo il "film" di cosa è successo prima del crash.
/// </summary>
public class ContextualLoggingEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/contextual").WithTags("Contextual Logging");

        // GET /api/contextual/simulate-error — simula attività normale + errore
        group.MapGet("/simulate-error", (ILogger<ContextualLoggingEndpoints> logger) => {
            logger.LogDebug("Loading user preferences from cache");
            logger.LogInformation("Processing payment for Order {OrderId}, Amount {Amount:C}", 1042, 89.99m);
            logger.LogDebug("Validating credit card token {TokenPrefix}...", "tok_4242****");
            logger.LogWarning("Payment gateway response slow: {Latency}ms", 2350);
            logger.LogInformation("Retrying payment via fallback gateway");

            try {
                throw new TimeoutException("Fallback gateway did not respond within 5000ms");
            } catch (Exception ex) {
                logger.LogError(ex, "Payment failed for Order {OrderId} after retry", 1042);
            }

            return Results.Ok(new {
                message = "Error simulated — check Console/File sink for contextual history entries",
                hint = "Look for [Context before error] lines preceding the Error log"
            });
        })
        .WithSummary("Simulate activity + error to trigger contextual flush")
        .WithDescription(
            "Generates 5 log entries (Debug, Info, Debug, Warning, Info) that accumulate in the ring buffer, " +
            "then throws an Error. The ContextualLogSink detects the Error and flushes all buffered entries " +
            "with [Context before error] prefix, giving you the full story leading up to the crash. " +
            "Requires EnableContextualLogging=true in appsettings.");

        // GET /api/contextual/generate-activity — genera log senza errori (riempie il buffer)
        group.MapGet("/generate-activity", (ILogger<ContextualLoggingEndpoints> logger) => {
            logger.LogDebug("Background job scheduler tick at {Timestamp}", DateTime.UtcNow);
            logger.LogInformation("Health check: database connection OK, latency {Latency}ms", 12);
            logger.LogInformation("Cache hit ratio: {Ratio}%", 94.5);
            logger.LogDebug("Checking message queue depth: {Depth} pending", 3);
            logger.LogWarning("Memory pressure: GC Gen2 collections = {Count}", 7);
            logger.LogInformation("User {UserId} logged in from {IpAddress}", "usr_abc123", "192.168.1.42");

            return Results.Ok(new {
                message = "6 log entries added to the contextual ring buffer",
                hint = "Call /api/contextual/simulate-error next to see these entries flushed with the error"
            });
        })
        .WithSummary("Generate normal activity logs (fills the ring buffer)")
        .WithDescription(
            "Writes 6 Debug/Info/Warning entries into the ring buffer WITHOUT triggering an error. " +
            "Use this to pre-fill the buffer, then call /simulate-error to see all buffered entries " +
            "flushed together with the error — demonstrating the 'context window' behavior.");

        // GET /api/contextual/buffer-status — stato del buffer
        group.MapGet("/buffer-status", (IServiceProvider sp) => {
            var buffer = sp.GetService<ContextualLogBuffer>();
            if (buffer is null)
                return Results.Json(new {
                    enabled = false,
                    message = "Contextual logging is not enabled. Set General.EnableContextualLogging=true in appsettings."
                });

            return Results.Json(new {
                enabled = true,
                count = buffer.Count,
                capacity = buffer.Capacity,
                totalPushes = buffer.TotalPushes,
                entries = buffer.Snapshot().Select(e => new {
                    timestamp = e.Timestamp.ToString("HH:mm:ss.fff"),
                    level = e.Level.ToString(),
                    source = e.SourceContext,
                    message = e.Message
                })
            });
        })
        .WithSummary("View ring buffer status and current entries")
        .WithDescription(
            "Returns the current state of the contextual ring buffer: how many entries are buffered, " +
            "total pushes since startup, and a snapshot of all current entries. " +
            "Note: after /simulate-error, the buffer is cleared (flushed with the error context).");

        // GET /api/contextual/multi-error — errori multipli in sequenza
        group.MapGet("/multi-error", (ILogger<ContextualLoggingEndpoints> logger) => {
            logger.LogInformation("Starting batch import of {Count} records", 500);
            logger.LogDebug("Parsing CSV row {Row}: {Data}", 1, "valid");
            logger.LogDebug("Parsing CSV row {Row}: {Data}", 2, "valid");
            logger.LogWarning("CSV row {Row} has missing field: {Field}", 3, "email");

            try {
                throw new FormatException("Invalid date format in CSV row 4: '31/13/2024'");
            } catch (Exception ex) {
                logger.LogError(ex, "Batch import failed at row {Row}", 4);
            }

            logger.LogInformation("Restarting batch import with skip-on-error mode");
            logger.LogDebug("Skipping invalid rows, processing remaining {Count}", 496);

            try {
                throw new InvalidOperationException("Database connection pool exhausted during bulk insert");
            } catch (Exception ex) {
                logger.LogError(ex, "Batch import failed during bulk insert");
            }

            return Results.Ok(new {
                message = "Two errors simulated with context — demonstrates buffer refill between errors",
                hint = "First error flushes context (4 entries), then new logs accumulate, second error flushes again (2 entries)"
            });
        })
        .WithSummary("Simulate two errors with different context windows")
        .WithDescription(
            "Demonstrates that the ring buffer refills after each flush. First generates 4 entries + error " +
            "(context flushed), then 2 more entries + second error (new context flushed). " +
            "Shows that each error gets its own 'context window' of preceding activity.");
    }
}