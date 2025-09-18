using CSharpEssentials.LoggerHelper.AI.Doamin;
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI;
public sealed class CorrelateTraceAction : ILogMacroAction {
    private readonly ILogRepository _logs; private readonly ITraceRepository _traces;
    public string Name => "CorrelateTrace";
    public CorrelateTraceAction(ILogRepository logs, ITraceRepository traces) { _logs = logs; _traces = traces; }
    public bool CanExecute(MacroContext ctx) => string.IsNullOrEmpty(ctx.TraceId); // lo scopre
    public async Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default) {
        var recent = new List<TraceRecord>();
        await foreach (var t in _traces.GetRecentAsync(50).WithCancellation(ct))
            recent.Add(t);

        // euristica semplice: primo trace con errori
        var hit = recent.FirstOrDefault(t => t.Error);

        return new MacroResult(Name,
            hit is null
                ? "Nessun trace con errori recenti."
                : $"Trace candidato: {hit.TraceId} servizio {hit.Service} durata {hit.Duration}.",
            hit is null ? null : new() { ["traceId"] = hit.TraceId });
    }
}