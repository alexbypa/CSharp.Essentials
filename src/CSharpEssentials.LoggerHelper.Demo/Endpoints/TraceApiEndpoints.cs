using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 2: API BeginTrace e Trace — enrichment con IdTransaction e Action.
/// Dimostra la feature principale di LoggerHelper: tracciamento per transazione.
/// </summary>
public class TraceApiEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/trace").WithTags("Trace API");

        // GET /api/trace/scope — BeginTrace scope (raccomandato per operazioni multi-step)
        group.MapGet("/scope", (ILogger<TraceApiEndpoints> logger) => {
            var txnId = $"TXN-{Guid.NewGuid():N}"[..12];

            using (logger.BeginTrace("OrderProcess", txnId)) {
                logger.LogInformation("Step 1: Validating order {OrderId}", 42);
                logger.LogInformation("Step 2: Reserving stock for {ProductId}", "SKU-100");
                logger.LogWarning("Step 3: Low inventory — only {Qty} left", 2);
                logger.LogInformation("Step 4: Charging {Amount:C}", 49.99m);
                logger.LogInformation("Step 5: Order confirmed");
            }
            // Log fuori scope — NON ha IdTransaction/Action
            logger.LogInformation("Outside scope — no enrichment");

            return Results.Ok(new {
                message = "5 logs with IdTransaction+Action inside scope, 1 without",
                idTransaction = txnId
            });
        });

        // GET /api/trace/single-shot — Trace one-off (per log isolati)
        group.MapGet("/single-shot", (ILogger<TraceApiEndpoints> logger) => {
            var txnId = $"TXN-{Guid.NewGuid():N}"[..12];

            logger.Trace("PaymentProcess", txnId,
                LogLevel.Information, null,
                "Payment {PaymentId} processed for {Amount:C}", "PAY-001", 99.90m);

            logger.Trace("PaymentProcess", txnId,
                LogLevel.Error,
                new InvalidOperationException("Insufficient funds"),
                "Payment {PaymentId} failed", "PAY-002");

            return Results.Ok(new {
                message = "2 single-shot logs with inline IdTransaction+Action",
                idTransaction = txnId
            });
        });

        // GET /api/trace/shorthand — Trace shorthand (Information, no exception)
        group.MapGet("/shorthand", (ILogger<TraceApiEndpoints> logger) => {
            var txnId = $"TXN-{Guid.NewGuid():N}"[..12];

            logger.Trace("QuickAction", txnId, "Quick log {Item}", "Widget");

            return Results.Ok(new {
                message = "1 shorthand trace (Information level, no exception)",
                idTransaction = txnId
            });
        });
    }
}
