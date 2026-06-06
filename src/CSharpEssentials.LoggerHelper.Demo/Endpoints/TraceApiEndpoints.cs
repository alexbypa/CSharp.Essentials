using CSharpEssentials.LoggerHelper;
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
        })
        .WithSummary("BeginTrace scope — enrich a multi-step operation")
        .WithDescription(
            "Opens a BeginTrace scope with a unique IdTransaction and Action name. " +
            "All 5 log entries inside the using block carry both fields; the final entry outside does not. " +
            "The response includes the generated IdTransaction so you can search for it in your sinks. " +
            "This is the recommended pattern for tracing multi-step workflows (order processing, imports, etc.).");

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
        })
        .WithSummary("Single-shot trace — one-off log with IdTransaction")
        .WithDescription(
            "Uses logger.Trace(action, txnId, level, ex, template, args) to emit individual logs " +
            "with inline IdTransaction and Action — no using block needed. " +
            "Useful when you want to tag a single event without wrapping it in a scope. " +
            "Emits one Information log and one Error log with an attached exception.");

        // GET /api/trace/shorthand — Trace shorthand (Information, no exception)
        group.MapGet("/shorthand", (ILogger<TraceApiEndpoints> logger) => {
            var txnId = $"TXN-{Guid.NewGuid():N}"[..12];

            logger.Trace("QuickAction", txnId, "Quick log {Item}", "Widget");

            return Results.Ok(new {
                message = "1 shorthand trace (Information level, no exception)",
                idTransaction = txnId
            });
        })
        .WithSummary("Shorthand trace — minimal syntax, Information level")
        .WithDescription(
            "Uses the convenience overload logger.Trace(action, txnId, template, args) " +
            "which defaults to Information level with no exception. " +
            "Minimal syntax for quick one-line tracing when you only need to tag a log with a transaction ID.");
    }
}
