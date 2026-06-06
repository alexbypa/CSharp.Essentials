using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 1: Logging base con ILogger standard.
/// Dimostra che LoggerHelper funziona come drop-in replacement di qualsiasi ILogger provider.
/// </summary>
public class BasicLoggingEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/basic").WithTags("Basic Logging");

        // GET /api/basic/info — log Information
        group.MapGet("/info", (ILogger<BasicLoggingEndpoints> logger) => {
            logger.LogInformation("Simple info message at {Timestamp}", DateTime.UtcNow);
            return Results.Ok(new { message = "Logged Information level" });
        })
        .WithSummary("Log at Information level")
        .WithDescription(
            "Writes a single LogInformation entry with a UTC timestamp. " +
            "Verifies that LoggerHelper works as a drop-in replacement for any ILogger provider — " +
            "check the Console and Logs/ folder to see the output.");

        // GET /api/basic/levels — tutti i livelli
        group.MapGet("/levels", (ILogger<BasicLoggingEndpoints> logger) => {
            logger.LogTrace("This is Trace");
            logger.LogDebug("This is Debug");
            logger.LogInformation("This is Information");
            logger.LogWarning("This is Warning");
            logger.LogError("This is Error");
            logger.LogCritical("This is Critical");
            return Results.Ok(new { message = "All 6 levels logged — check which sinks received which levels" });
        })
        .WithSummary("Emit all 6 log levels")
        .WithDescription(
            "Writes one log at each level: Trace, Debug, Information, Warning, Error, Critical. " +
            "Use this together with /api/routing/test to verify that each sink receives only the levels " +
            "defined in its Routes entry in appsettings.LoggerHelper.json.");

        // GET /api/basic/structured — structured logging con proprietà
        group.MapGet("/structured", (ILogger<BasicLoggingEndpoints> logger) => {
            logger.LogInformation(
                "Order {OrderId} placed by {Customer} for {Amount:C}",
                42, "Acme Corp", 129.99m);
            return Results.Ok(new { message = "Structured log with OrderId, Customer, Amount" });
        })
        .WithSummary("Structured log with named properties")
        .WithDescription(
            "Logs an order event using Serilog message templates: {OrderId}, {Customer}, {Amount}. " +
            "Each placeholder becomes a first-class searchable field in structured sinks " +
            "(MSSqlServer, PostgreSQL, Elasticsearch, Seq) — not just a flattened string.");

        // GET /api/basic/exception — log con eccezione
        group.MapGet("/exception", (ILogger<BasicLoggingEndpoints> logger) => {
            try {
                throw new InvalidOperationException("Payment gateway timeout");
            } catch (Exception ex) {
                logger.LogError(ex, "Payment failed for Order {OrderId}", 42);
            }
            return Results.Ok(new { message = "Exception logged at Error level" });
        })
        .WithSummary("Log an exception at Error level")
        .WithDescription(
            "Catches a simulated InvalidOperationException and logs it with LogError(ex, ...). " +
            "The full exception type, message, and stack trace are captured as structured fields " +
            "in sinks that support it (MSSqlServer, PostgreSQL, Elasticsearch).");
    }
}
