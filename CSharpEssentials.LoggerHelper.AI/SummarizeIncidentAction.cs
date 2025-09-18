using CSharpEssentials.LoggerHelper.AI.Doamin;
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI;
public sealed class SummarizeIncidentAction : ILogMacroAction {
    private readonly ILogRepository _logs; private readonly ILlmChat _llm;
    public string Name => "SummarizeIncident";
    public SummarizeIncidentAction(ILogRepository logs, ILlmChat llm) { _logs = logs; _llm = llm; }

    public bool CanExecute(MacroContext ctx) => !string.IsNullOrEmpty(ctx.TraceId);
    public async Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default) {
        var buf = new List<string>();
        int n = 0;
        await foreach (var r in _logs.SearchAsync(ctx.TraceId!, 200).WithCancellation(ct)) {
            buf.Add($"[{r.Ts:u}] {r.Level} {r.Content}");
            if (++n >= 50)
                break;
        }
        var context = string.Join("\n", buf);
        var system = "Sei un SRE assistant. Rispondi in 8-10 righe, con elenco puntato per cause e fix.";
        var user = $"Trace: {ctx.TraceId}\nLog:\n{context}";
        var summary = await _llm.ChatAsync(system, user, 0.0);
        return new MacroResult(Name, summary, new() { ["traceId"] = ctx.TraceId!, ["count"] = n });
    }
}