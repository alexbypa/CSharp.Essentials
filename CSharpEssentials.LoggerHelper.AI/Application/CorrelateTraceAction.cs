using CSharpEssentials.LoggerHelper.AI.Domain;   // fix del typo
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class CorrelateTraceAction : ILogMacroAction {
    private readonly ILogRepository _logs;
    private readonly ITraceRepository _traces;
    public string Name => "CorrelateTrace";
    private readonly ILlmChat _llm;
    public CorrelateTraceAction(ILogRepository logs, ITraceRepository traces, ILlmChat llm) {
        _logs = logs;
        _traces = traces;
        _llm = llm;
    }

    public bool CanExecute(MacroContext ctx) => string.IsNullOrEmpty(ctx.TraceId); // lo scopre

    public async Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default) {
        // _traces.GetRecentAsync ritorna Task<IReadOnlyList<TraceRecord>>
        var recent = await _traces.GetRecentAsync(50, ct);

        //// scegli il predicato coerente col tuo model (es. Anomaly == true)
        //var hit = recent.FirstOrDefault(t => t.Anomaly == true);

        var lines = recent.Select(t =>$"{t.TraceId} | name={t.Name} | dur={t.Duration.TotalMilliseconds:F0}ms | anomaly={(t.Anomaly == true ? 1 : 0)} | tags={t.TagsJson}");

        //var system = "You are an SRE assistant. Pick the most suspicious trace and explain why.";
        var system = ctx.Query;

        var user = "Candidates:\n" + string.Join("\n", lines) + "\nReturn: traceId + short reason.";
        var pick = await _llm.ChatAsync(system, user);

        // estrai traceId dalla risposta (regex semplice)
        //var id = ExtractTraceId(pick);
        //var hit = recent.FirstOrDefault(t => t.TraceId == id);

        return new MacroResult(
            Name,
            pick
        );
    }
}