using CSharpEssentials.LoggerHelper.AI.Application;
using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CSharpEssentials.LoggerHelper.AI.Endpoints;
[ApiController]
[Route("api")]
public class AIEndpoints : ControllerBase {
    [HttpPost("LLMQuery")]
    public async Task<IActionResult> LLMQuery(
        [FromServices] IActionOrchestrator orc,
        [FromBody] IMacroContext ctx,
        CancellationToken ct) {
        return Ok(await orc.RunAsync(ctx, ct));
    }
    [HttpPost("AISettings")]
    public async Task<IActionResult> AISettings([FromServices] List<SQLLMModels> sQLLMModels) {
        await Task.CompletedTask;
        return Ok(sQLLMModels);
    }
}
