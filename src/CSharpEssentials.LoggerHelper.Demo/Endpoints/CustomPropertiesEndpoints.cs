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
        });

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
        });

        // GET /api/properties/inline — proprietà nel messaggio template
        group.MapGet("/inline", (ILogger<CustomPropertiesEndpoints> logger) => {
            logger.LogInformation(
                "User {UserId} performed {Action} on {Resource} in {Region}",
                "U-100", "DELETE", "/api/orders/42", "US-East");

            return Results.Ok(new { message = "Log with 4 inline structured properties" });
        });
    }
}
