using CSharpEssentials.LoggerHelper.AI.Domain;
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

        // se esiste l’overload con ct, usalo
        var records = await _logs.SearchAsync(ctx.TraceId!, 200);
        // altrimenti: var records = await _logs.SearchAsync(ctx.TraceId!, 200);

        foreach (var r in records) {
            buf.Add($"{r.Ts:u} {r.Level} {r.Message}");
            if (++n >= 50)
                break;
        }

        var context = string.Join("\n", buf);
        var system = "Sei un SRE assistant.";
        var user = $"Trace: {ctx.TraceId}\n{context}";
        var summary = await _llm.ChatAsync(system, user);

        return new MacroResult(Name, summary, new() { ["traceId"] = ctx.TraceId!, ["count"] = n });
    }
}