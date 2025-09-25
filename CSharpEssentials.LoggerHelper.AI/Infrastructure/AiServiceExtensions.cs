using CSharpEssentials.LoggerHelper.AI.Application;
using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

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
        // Dipendono da 'IWrapperDbConnection' che abbiamo registrato sopra.
        // -> Quando una classe chiede 'ILogRepository', gli viene data un'istanza di 'SqlLogRepository'.
        services.AddScoped<ILogRepository, SqlLogRepository>();
        services.AddScoped<ITraceRepository<TraceRecord>, SqlTraceRepository>();
        services.AddScoped<IMetricRepository, SqlMetricRepository>();

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

        // 1. Registra le implementazioni concrete una sola volta.
        services.AddScoped<SummarizeIncidentAction>();
        services.AddScoped<CorrelateTraceAction>();
        services.AddScoped<DetectAnomalyAction>();
        services.AddScoped<RagAnswerQueryAction>();

        services.AddScoped<ILogMacroAction, SummarizeIncidentAction>(sp => sp.GetRequiredService<SummarizeIncidentAction>());
        //services.AddScoped<ILogMacroAction<SummarizeContext>, SummarizeIncidentAction>(sp => sp.GetRequiredService<SummarizeIncidentAction>());

        // --- Registrazione CorrelateTraceAction ---
        services.AddScoped<ILogMacroAction, CorrelateTraceAction>(sp => sp.GetRequiredService<CorrelateTraceAction>());
        //services.AddScoped<ILogMacroAction<CorrelateContext>, CorrelateTraceAction>(sp => sp.GetRequiredService<CorrelateTraceAction>());

        // --- Registrazione DetectAnomalyAction ---
        services.AddScoped<ILogMacroAction, DetectAnomalyAction>(sp => sp.GetRequiredService<DetectAnomalyAction>());
        //services.AddScoped<ILogMacroAction<DetectAnomalyContext>, DetectAnomalyAction>(sp => sp.GetRequiredService<DetectAnomalyAction>());

        // --- Registrazione RagAnswerQueryAction ---
        services.AddScoped<ILogMacroAction, RagAnswerQueryAction>(sp => sp.GetRequiredService<RagAnswerQueryAction>());
        //Sservices.AddScoped<ILogMacroAction<RagContext>, RagAnswerQueryAction>(sp => sp.GetRequiredService<RagAnswerQueryAction>());

        // -> Registra l'orchestratore, la classe che gestisce e coordina tutte le azioni.
        services.AddScoped<IActionOrchestrator, ActionOrchestrator>();
        services.AddScoped<ILlmChat, OpenAiLlmChat>(); // oppure

        return services;
    }
}

public static class AIServiceExtensions {
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapPost("/api/AISettings", (IFileLoader fileLoader) => fileLoader.getModelSQLLMModels()).ExcludeFromDescription();

        endpoints.MapPost("/api/LLMQuery", async (IActionOrchestrator orc, IMacroContext ctx, CancellationToken ct) =>
            Results.Ok(await orc.RunAsync(ctx, ct)))
                .ExcludeFromDescription();

        return endpoints;
    }
}