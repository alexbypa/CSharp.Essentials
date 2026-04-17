using CSharpEssentials.LoggerHelper;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════════
// OPZIONE A — registrazione via IServiceCollection (classica)
// ══════════════════════════════════════════════════════════════════
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("TestApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .EnableRequestResponseLogging()
);

// ══════════════════════════════════════════════════════════════════
// OPZIONE B — registrazione via ILoggingBuilder (rispetta i filtri
//             "Logging:LogLevel" di appsettings.json)
// Decommenta questa e commenta l'OPZIONE A per provarla.
// ══════════════════════════════════════════════════════════════════
// builder.Logging.ClearProviders();
// builder.Logging.AddLoggerHelper(b => b
//     .WithApplicationName("TestApp")
//     .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
//     .EnableRequestResponseLogging()
// );

var app = builder.Build();
app.UseLoggerHelper();

// ──────────────────────────────────────────────────────────────────
// Endpoint 1 — uso base di ILogger<T>
// Dimostra che ILogger<T> funziona senza cambiare il codice esistente
// ──────────────────────────────────────────────────────────────────
app.MapGet("/", (ILogger<Program> logger) => {
    logger.LogInformation("Hello from LoggerHelper v5!");
    logger.LogWarning("This is a warning test");
    logger.LogError("This is an error test");
    return "LoggerHelper v5 is working!";
});

// ──────────────────────────────────────────────────────────────────
// Endpoint 2 — BeginScope con properties di business
//
// Tutti i log dentro l'using portano automaticamente OrderId e UserId.
// Senza BeginScope dovresti passarli a mano ad ogni riga di log.
// ──────────────────────────────────────────────────────────────────
app.MapGet("/orders/{orderId}", (int orderId, ILogger<Program> logger) => {
    var userId = "user-42"; // in un caso reale verrebbe da HttpContext o claim

    using (logger.BeginScope(new Dictionary<string, object?> {
        ["OrderId"] = orderId,
        ["UserId"] = userId
    }))
    {
        logger.LogInformation("Elaborazione ordine avviata");
        // output: {"Message":"Elaborazione ordine avviata","OrderId":123,"UserId":"user-42",...}

        SimulaProcessoOrdine(logger, orderId);
        // anche i log dentro questo metodo portano OrderId e UserId

        logger.LogInformation("Elaborazione ordine completata");
    }
    // qui fuori dall'using: OrderId e UserId spariscono dallo scope

    return $"Ordine {orderId} processato";
});

// ──────────────────────────────────────────────────────────────────
// Endpoint 3 — scope annidati
//
// Dimostra che gli scope si accumulano: il log finale porta
// sia le properties dell'ordine che quelle del pagamento.
// ──────────────────────────────────────────────────────────────────
app.MapGet("/orders/{orderId}/pay", (int orderId, ILogger<Program> logger) => {
    using (logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = orderId }))
    {
        logger.LogInformation("Avvio pagamento ordine");
        // output: {"Message":"Avvio pagamento ordine","OrderId":123}

        using (logger.BeginScope(new Dictionary<string, object?> { ["PaymentProvider"] = "Stripe", ["Amount"] = 99.90 }))
        {
            logger.LogInformation("Chiamata provider pagamento");
            // output: {"Message":"Chiamata provider pagamento","OrderId":123,"PaymentProvider":"Stripe","Amount":99.90}

            logger.LogWarning("Tentativo 2/3 — provider lento");
            // output: ha ancora tutte e tre le properties
        }

        logger.LogInformation("Pagamento completato");
        // output: {"Message":"Pagamento completato","OrderId":123}
        // PaymentProvider e Amount non ci sono più — scope interno chiuso
    }

    return $"Pagamento ordine {orderId} ok";
});

app.Run();

// Metodo helper — dimostra che BeginScope propaga anche nei metodi chiamati
static void SimulaProcessoOrdine(ILogger logger, int orderId) {
    logger.LogInformation("Verifica stock in magazzino");
    // Questo log porta OrderId e UserId anche se non li passiamo esplicitamente

    if (orderId > 999)
        logger.LogWarning("Ordine di grandi dimensioni — richiede approvazione manuale");
}

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
