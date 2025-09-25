using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Infrastructure;
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public sealed class SummarizeIncidentAction : ILogMacroAction<SummarizeContext> {
    //private readonly ILogRepository _logs; 
    private readonly ILlmChat _llm;
    private readonly ISqlQueryWrapper _sqlQueryWrapper;
    public string Name => "SummarizeIncident";
    public Type ContextType => typeof(SummarizeContext);
    public SummarizeIncidentAction(ISqlQueryWrapper sqlQueryWrapper, ILlmChat llm) { sqlQueryWrapper = _sqlQueryWrapper; _llm = llm; }
    public bool CanExecute(IMacroContext ctx) => !string.IsNullOrEmpty(ctx.TraceId);
    public async Task<MacroResult> ExecuteAsync(IMacroContext ctx, CancellationToken ct = default) {
        // 1) Fetch logs for the given trace
        // Use the dedicated API for correlation by TraceId
        var records = await _sqlQueryWrapper.QueryAsync(ctx.TraceId!, 200);

        // 2) Build a compact, chronologically ordered timeline
        //var lines = records.Select(x => $"{x.TimeStamp:u} {x.Level} {Compact(x.Message)}").ToList();

        TraceFormatterService _formatter;
        string myFormatTemplate = "{x.TimeStamp:u} {x.Level} {Compact(x.Message)}";
        _formatter = new TraceFormatterService(myFormatTemplate);
        var contextBlock = _formatter.Format(records);

        // 3) Enforce a simple token/char budget
        //var timeline = TakeForTokenBudget(contextBlock, maxChars: 4000); 

        // 4) Compose prompts
        //var system = "You are an SRE assistant. Summarize root cause, impact, key signals, and remediation. Be concise.";
        //var user = $"Trace: {ctx.TraceId}\n{string.Join("\n", timeline)}";
        var messages = new List<ChatPromptMessage>
        {
            new("system", "You are an SRE assistant. Summarize root cause, impact, key signals, and remediation. Be concise."),
            // Example few‑shot Q/A pair to guide tone and structure
            new("user",      "Esempio di richiesta: sintetizza un problema di latenza e suggerisci rimedi."),
            new("assistant", "Sintesi: L'applicazione ha sperimentato una latenza elevata a causa di un sovraccarico del database. Impatto: gli utenti hanno registrato tempi di risposta lenti. Segnali: picchi di CPU e query lente. Remediation: scalare il database e ottimizzare le query."),
            // Actual user request with context
            new("user",     $"Trace: {ctx.TraceId}\n{string.Join("\n", contextBlock)}")
        };

        // 5) Call the LLM
        //var summary = await _llm.ChatAsync(system, user);
        var summary = await _llm.ChatAsync(messages);

        return new MacroResult(Name, summary, new() { ["traceId"] = ctx.TraceId!, ["count"] = 0 /*lines.Count //TODO: */ });
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