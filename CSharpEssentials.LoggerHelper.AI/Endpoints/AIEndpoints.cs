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
        [FromBody] MacroContext ctx,
        CancellationToken ct) {
        return Ok(await orc.RunAsync(ctx, ct));
    }
    [HttpPost("AISettings")]
    public async Task<IActionResult> AISettings([FromServices] LoggerAIOptions opt) {
        var list = new List<SqlQueryFile> {
        new SqlQueryFile { action = "SummarizeIncident", FileName = "test1.sql", Content = "select 1"},
        new SqlQueryFile { action = "CorrelateTrace",  FileName = "test2.sql", Content = "select 2"},
        new SqlQueryFile { action = "DetectAnomaly",  FileName = "test3.sql", Content = "select 3"},
        new SqlQueryFile { action = "RagAnswerQuery",  FileName = "test4.sql", Content = "select Id, ApplicationName App, TimeStamp Ts, LogEvent Message, IdTransaction TraceId from [LogEntry]  order by id desc"}
        };
        await Task.CompletedTask;
        return Ok(list);
    }
}
class SqlQueryFile {
    public string? action { get; set; }
    public string? FileName { get; set; }
    public string? Content { get; set; }
}