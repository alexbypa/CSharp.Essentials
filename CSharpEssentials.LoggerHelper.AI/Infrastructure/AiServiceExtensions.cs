using CSharpEssentials.LoggerHelper.AI.Application;
using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
// Nel progetto CSharpEssentials.LoggerHelper.AI
public static class AIServiceExtensions {
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints) {

        endpoints.MapPost("/api/AISettings", () => {
            return Results.Ok(new List<actionsLLMQuery> {
                new actionsLLMQuery { action = "Correlate Trace", content = "select 1....", fileName = "file1" },
                new actionsLLMQuery { action = "Detect Anomaly", content = "select 2....", fileName = "file1" },
                new actionsLLMQuery { action = "Summarize Incident", content = "select 3....", fileName = "file1" },
                new actionsLLMQuery { action = "Rag Answer Query", content = "select 4....", fileName = "file1" },
            });
        })
            .ExcludeFromDescription();

        endpoints.MapPost("/api/LLMQuery", async (IActionOrchestrator orc, MacroContext ctx, CancellationToken ct) =>
            Results.Ok(await orc.RunAsync(ctx, ct)))
                .ExcludeFromDescription();

        return endpoints;
    }

    private class actionsLLMQuery {
        public string content { get; set; }
        public string action { get; set; }
        public string fileName { get; set; }
    }
}