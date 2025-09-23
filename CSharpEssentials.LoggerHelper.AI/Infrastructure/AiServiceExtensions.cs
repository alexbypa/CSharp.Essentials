using CSharpEssentials.LoggerHelper.AI.Application;
using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
public static class AIServiceExtensions {
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapPost("/api/AISettings", (IFileLoader fileLoader) => fileLoader.getModelSQLLMModels()).ExcludeFromDescription();

        endpoints.MapPost("/api/LLMQuery", async (IActionOrchestrator orc, MacroContext ctx, CancellationToken ct) =>
            Results.Ok(await orc.RunAsync(ctx, ct)))
                .ExcludeFromDescription();

        return endpoints;
    }
}