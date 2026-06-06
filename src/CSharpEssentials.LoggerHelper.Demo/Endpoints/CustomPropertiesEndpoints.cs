using CSharpEssentials.LoggerHelper;
using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 3: Proprietà custom aggiuntive.
/// Dimostra come aggiungere proprietà arbitrarie ai log usando BeginScope standard.
/// </summary>
public class CustomPropertiesEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/properties").WithTags("Custom Properties");

        // GET /api/properties/scope — proprietà custom via BeginScope
        group.MapGet("/scope", (ILogger<CustomPropertiesEndpoints> logger) => {
            using (logger.BeginScope(new KeyValuePair<string, object?>[] {
                new("CustomerId", "C-42"),
                new("Region", "EU-West"),
                new("TenantId", "tenant-acme")
            })) {
                logger.LogInformation("Customer {CustomerId} logged in from {IpAddress}", "C-42", "192.168.1.1");
                logger.LogWarning("Rate limit approaching for tenant");
            }

            return Results.Ok(new { message = "2 logs enriched with CustomerId, Region, TenantId" });
        })
        .WithSummary("Add custom properties via BeginScope")
        .WithDescription(
            "Wraps two log entries in BeginScope with a KeyValuePair array containing CustomerId, Region, and TenantId. " +
            "All three properties appear as first-class fields in structured sinks alongside the standard Serilog fields. " +
            "This pattern is compatible with any ILogger provider — no LoggerHelper-specific APIs required.");

        // GET /api/properties/nested — scope annidati (BeginTrace + custom)
        group.MapGet("/nested", (ILogger<CustomPropertiesEndpoints> logger) => {
            var txnId = $"TXN-{Guid.NewGuid():N}"[..12];

            // Scope esterno: IdTransaction + Action
            using (logger.BeginTrace("ImportProcess", txnId)) {
                logger.LogInformation("Import started");

                // Scope interno: aggiunge proprietà extra
                using (logger.BeginScope(new KeyValuePair<string, object?>[] {
                    new("BatchId", "BATCH-001"),
                    new("RowCount", 1500)
                })) {
                    logger.LogInformation("Processing batch of {Count} rows", 1500);
                    logger.LogWarning("Skipped {Skipped} duplicate rows", 3);
                }
                // Fuori dal scope interno: solo IdTransaction + Action
                logger.LogInformation("Import completed");
            }

            return Results.Ok(new {
                message = "Nested scopes — inner has both trace + custom properties",
                idTransaction = txnId
            });
        })
        .WithSummary("Nested scopes — combine BeginTrace with custom properties")
        .WithDescription(
            "Demonstrates scope nesting: the outer BeginTrace scope adds IdTransaction and Action to all inner logs; " +
            "the inner BeginScope adds BatchId and RowCount only to the two logs inside it. " +
            "The final 'Import completed' log carries IdTransaction + Action but not BatchId/RowCount. " +
            "Use this pattern for batch jobs, imports, or any operation with sub-phases.");

        // GET /api/properties/inline — proprietà nel messaggio template
        group.MapGet("/inline", (ILogger<CustomPropertiesEndpoints> logger) => {
            logger.LogInformation(
                "User {UserId} performed {Action} on {Resource} in {Region}",
                "U-100", "DELETE", "/api/orders/42", "US-East");

            return Results.Ok(new { message = "Log with 4 inline structured properties" });
        })
        .WithSummary("Inline structured properties in the message template")
        .WithDescription(
            "Logs a single audit event with 4 named holes in the message template: " +
            "{UserId}, {Action}, {Resource}, {Region}. " +
            "No scope needed — each placeholder is captured as a separate searchable field. " +
            "This is the cleanest syntax for one-off structured events where you don't need a correlation ID.");
    }
}
