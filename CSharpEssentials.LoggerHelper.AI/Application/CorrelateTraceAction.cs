using CSharpEssentials.LoggerHelper.AI.Domain;   // fix del typo
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class CorrelateTraceAction : ILogMacroAction {
    private readonly ILogRepository _logs;
    private readonly ITraceRepository<TraceRecord> _traces;
    public string Name => "CorrelateTrace";
    private readonly ILlmChat _llm;
    private readonly List<SQLLMModels> _sQLLMModels;
    public CorrelateTraceAction(ILogRepository logs, ITraceRepository<TraceRecord> traces, ILlmChat llm, List<SQLLMModels> sQLLMModels) {
        _logs = logs;
        _traces = traces;
        _llm = llm;
        _sQLLMModels = sQLLMModels;
    }

    public bool CanExecute(MacroContext ctx) => !string.IsNullOrEmpty(ctx.TraceId); // lo scopre

    public async Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default) {
        // _traces.GetRecentAsync ritorna Task<IReadOnlyList<TraceRecord>>
        // var recent = await _traces.GetRecentAsync(50, ct);
        var sqlQuery = _sQLLMModels.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == ctx.fileName)?.content;
        var traceRecords = await _traces.GetByTraceIdAsync(sqlQuery, ctx.TraceId);

        //// scegli il predicato coerente col tuo model (es. Anomaly == true)
        //var hit = recent.FirstOrDefault(t => t.Anomaly == true);

        var lines = traceRecords.Select(t => $"{t.TraceId} | Span Name={t.Name} | duration={t.Duration:F0}ms | tags={t.TagsJson}");

        var contextBlock = string.Join("\n---\n", lines.Select(h => h));

        var messages = new[]{
            new ChatPromptMessage("system", ctx.system),
            new ChatPromptMessage("assistant", $"CONTEXT:\n{contextBlock}"),
            new ChatPromptMessage("user", $"Question: {ctx.Query}")
        };
        var answer = await _llm.ChatAsync(messages);

        return new MacroResult(Name, answer);
        
    }
}