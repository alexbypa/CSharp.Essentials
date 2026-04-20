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
        });

        // GET /api/basic/levels — tutti i livelli
        group.MapGet("/levels", (ILogger<BasicLoggingEndpoints> logger) => {
            logger.LogTrace("This is Trace");
            logger.LogDebug("This is Debug");
            logger.LogInformation("This is Information");
            logger.LogWarning("This is Warning");
            logger.LogError("This is Error");
            logger.LogCritical("This is Critical");
            return Results.Ok(new { message = "All 6 levels logged — check which sinks received which levels" });
        });

        // GET /api/basic/structured — structured logging con proprietà
        group.MapGet("/structured", (ILogger<BasicLoggingEndpoints> logger) => {
            logger.LogInformation(
                "Order {OrderId} placed by {Customer} for {Amount:C}",
                42, "Acme Corp", 129.99m);
            return Results.Ok(new { message = "Structured log with OrderId, Customer, Amount" });
        });

        // GET /api/basic/exception — log con eccezione
        group.MapGet("/exception", (ILogger<BasicLoggingEndpoints> logger) => {
            try {
                throw new InvalidOperationException("Payment gateway timeout");
            } catch (Exception ex) {
                logger.LogError(ex, "Payment failed for Order {OrderId}", 42);
            }
            return Results.Ok(new { message = "Exception logged at Error level" });
        });
    }
}
