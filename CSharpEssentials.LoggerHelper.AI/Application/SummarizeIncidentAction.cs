using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public sealed class SummarizeIncidentAction : ILogMacroAction {
    private readonly ILogRepository _logs; private readonly ILlmChat _llm;
    public string Name => "SummarizeIncident";
    public SummarizeIncidentAction(ILogRepository logs, ILlmChat llm) { _logs = logs; _llm = llm; }

    public bool CanExecute(MacroContext ctx) => !string.IsNullOrEmpty(ctx.TraceId);
    public async Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default) {
        // 1) Fetch logs for the given trace
        // Use the dedicated API for correlation by TraceId
        var records = await _logs.ByTraceAsync(ctx.TraceId!, 200);

        // 2) Build a compact, chronologically ordered timeline
        var lines = records
            .OrderBy(x => x.TimeStamp)                               // oldest -> newest
            .Select(x => $"{x.TimeStamp:u} {x.Level} {Compact(x.Message)}")
            .ToList();

        // 3) Enforce a simple token/char budget
        var timeline = TakeForTokenBudget(lines, maxChars: 4000);

        // 4) Compose prompts
        var system = "You are an SRE assistant. Summarize root cause, impact, key signals, and remediation. Be concise.";
        var user = $"Trace: {ctx.TraceId}\n{string.Join("\n", timeline)}";

        // 5) Call the LLM
        var summary = await _llm.ChatAsync(system, user);

        return new MacroResult(Name, summary, new() { ["traceId"] = ctx.TraceId!, ["count"] = lines.Count });
    }

    private static string Compact(string? s)
        => string.IsNullOrWhiteSpace(s) ? "" : (s.Length <= 300 ? s : s[..300] + "…");

    private static List<string> TakeForTokenBudget(List<string> lines, int maxChars) {
        // Keep last lines if needed, but ensure we don't exceed a rough char budget
        var outp = new List<String>();
        var total = 0;
        for (int i = lines.Count - 1; i >= 0; i--) {
            var l = lines[i];
            if (total + l.Length + 1 > maxChars)
                break;
            outp.Insert(0, l);
            total += l.Length + 1;
        }
        return outp;
    }
}