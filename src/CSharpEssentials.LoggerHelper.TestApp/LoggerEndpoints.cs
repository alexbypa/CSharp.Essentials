using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.TestApp;

public static class LoggerEndpoints
{
    public static void MapLoggerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Logger Use Cases");

        group.MapGet("/", IResult (ILogger<Program> logger) => {
            logger.LogInformation("Hello from LoggerHelper v5!");
            logger.LogWarning("This is a warning test");
            logger.LogError("This is an error test");
            return Results.Ok("LoggerHelper v5 is working!");
        })
        .WithName("BasicLog")
        .WithSummary("1. Uso base di ILogger<T>")
        .WithDescription("Dimostra che ILogger<T> funziona senza cambiare il codice esistente.");

        group.MapGet("/orders/{orderId}", IResult (int orderId, ILogger<Program> logger) => {
            var userId = "user-42"; // in un caso reale verrebbe da HttpContext o claim

            using (logger.BeginScope(new Dictionary<string, object?> {
                ["OrderId"] = orderId,
                ["UserId"] = userId
            }))
            {
                logger.LogInformation("Elaborazione ordine avviata");
                SimulaProcessoOrdine(logger, orderId);
                logger.LogInformation("Elaborazione ordine completata");
            }

            return Results.Ok($"Ordine {orderId} processato");
        })
        .WithName("ProcessOrder")
        .WithSummary("2. BeginScope con properties di business")
        .WithDescription("Tutti i log dentro l'using portano automaticamente OrderId e UserId. Senza BeginScope dovresti passarli a mano ad ogni riga di log.");

        group.MapGet("/orders/{orderId}/pay", IResult (int orderId, ILogger<Program> logger) => {
            using (logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = orderId }))
            {
                logger.LogInformation("Avvio pagamento ordine");

                using (logger.BeginScope(new Dictionary<string, object?> { ["PaymentProvider"] = "Stripe", ["Amount"] = 99.90 }))
                {
                    logger.LogInformation("Chiamata provider pagamento {nome}", "Ciccio");
                    logger.LogWarning("Tentativo 2/3 — provider lento");
                }

                logger.LogInformation("Pagamento completato");
            }

            return Results.Ok($"Pagamento ordine {orderId} ok");
        })
        .WithName("PayOrder")
        .WithSummary("3. Scope annidati")
        .WithDescription("Dimostra che gli scope si accumulano: il log finale porta sia le properties dell'ordine che quelle del pagamento.");

        var diagGroup = app.MapGroup("/diagnostics").WithTags("Diagnostics");

        diagGroup.MapGet("/", IResult (
            LoggerHelperOptions opts,
            ISinkPluginRegistry registry,
            ILogErrorStore errors) =>
        {
            return Results.Ok(new {
                ApplicationName = opts.ApplicationName,
                Routes = opts.Routes.Select(r => new { r.Sink, r.Levels }),
                RegisteredPlugins = registry.All.Select(p => p.GetType().Name),
                Errors = errors.GetAll().Select(e => new { e.SinkName, e.ErrorMessage, e.ContextInfo })
            });
        })
        .WithName("Diagnostics")
        .WithSummary("Endpoint diagnostico")
        .WithDescription("Mostra le opzioni lette dal JSON, sink registrati ed errori interni.");
    }

    private static void SimulaProcessoOrdine(ILogger logger, int orderId) {
        logger.LogInformation("Verifica stock in magazzino");
        // Questo log porta OrderId e UserId anche se non li passiamo esplicitamente

        if (orderId > 999)
            logger.LogWarning("Ordine di grandi dimensioni — richiede approvazione manuale");
    }
}
