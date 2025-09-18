using CSharpEssentials.LoggerHelper.AI.Domain;   // fix del typo
using CSharpEssentials.LoggerHelper.AI.Ports;

public sealed class CorrelateTraceAction : ILogMacroAction {
    private readonly ILogRepository _logs;
    private readonly ITraceRepository _traces;
    public string Name => "CorrelateTrace";

    public CorrelateTraceAction(ILogRepository logs, ITraceRepository traces) {
        _logs = logs;
        _traces = traces;
    }

    public bool CanExecute(MacroContext ctx) => string.IsNullOrEmpty(ctx.TraceId); // lo scopre

    public async Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default) {
        // _traces.GetRecentAsync ritorna Task<IReadOnlyList<TraceRecord>>
        var recent = await _traces.GetRecentAsync(50, ct);

        // scegli il predicato coerente col tuo model (es. Anomaly == true)
        var hit = recent.FirstOrDefault(t => t.Anomaly == true);

        return new MacroResult(
            Name,
            hit is null
                ? "Nessun trace con errori recenti."
                : $"Trace candidato: {hit.TraceId} servizio {hit.Name} durata {hit.Duration}.",
            hit is null ? null : new() { ["traceId"] = hit.TraceId }
        );
    }
}
