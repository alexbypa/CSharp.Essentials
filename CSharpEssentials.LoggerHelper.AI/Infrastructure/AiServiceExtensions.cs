using CSharpEssentials.LoggerHelper.AI.Application;
using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddCSharpEssentialsLoggerAI(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<IServiceCollection> configurePersistence) {
        services
            .AddOptions<LoggerAIOptions>()
            .Bind(configuration.GetSection("LoggerAIOptions"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        configurePersistence(services);
        // --- SEZIONE REPOSITORY (Livello Accesso Dati) ---
        // I repository sono classi che contengono la logica per interrogare il database.
        // Dipendono da 'FactorySQlConnection' che abbiamo registrato sopra.
        // -> Quando una classe chiede 'ILogRepository', gli viene data un'istanza di 'SqlLogRepository'.

        // -> Registra il servizio per creare gli embedding (vettori numerici dal testo).
        services.AddScoped<IEmbeddingService, NaiveEmbeddingService>();

        // -> Registra il nostro "costruttore di dati" da file. È 'Transient' perché è leggero,
        //    senza stato, e vogliamo un'istanza nuova ogni volta che viene usato.
        services.AddTransient<FileLogIndexer>(); // se vuoi usarlo per popolare il vettore store da file

        services.AddTransient<IFileLoader, FileLoader>();

        services.AddSingleton(sp => {
            var fileLoader = sp.GetRequiredService<IFileLoader>();
            return fileLoader.getModelSQLLMModels();
        });

        services.AddScoped<ILogVectorStore, SqlLogVectorStore>();

        services.AddScoped<ILogMacroAction, SummarizeIncidentAction>();
        services.AddScoped<ILogMacroAction, CorrelateTraceAction>();
        services.AddScoped<ILogMacroAction, DetectAnomalyAction>();
        services.AddScoped<ILogMacroAction, RagAnswerQueryAction>();

        // -> Registra l'orchestratore, la classe che gestisce e coordina tutte le azioni.
        services.AddScoped<IActionOrchestrator, ActionOrchestrator>();
        services.AddScoped<ILlmChat, OpenAiLlmChat>(); // oppure

        return services;
    }
}

public static class AIServiceExtensions {
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapPost("/api/AISettings", (IFileLoader fileLoader) => fileLoader.getModelSQLLMModels()).ExcludeFromDescription();

        endpoints.MapPost("/api/LLMQuery", async (IActionOrchestrator orc, MacroContextBase ctx, CancellationToken ct) =>
            Results.Ok(await orc.RunAsync(ctx, ct)))
                .ExcludeFromDescription();

        return endpoints;
    }
}