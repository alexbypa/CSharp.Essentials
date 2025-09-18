using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI;

public sealed class DetectAnomalyAction : ILogMacroAction {
    private readonly IMetricRepository _metrics;
    public string Name => "DetectAnomaly";
    public DetectAnomalyAction(IMetricRepository m) => _metrics = m;
    public bool CanExecute(MacroContext ctx) => true;
    public async Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default) {
        var to = ctx.Now;
        var from = to.AddMinutes(-30);
        var series = new List<(DateTimeOffset Time, double Value)>();

        // se esiste l’overload con ct
        var points = await _metrics.QueryAsync("http_5xx_rate", from, to);
        // altrimenti: var points = await _metrics.QueryAsync("http_5xx_rate", from, to);

        foreach (var t in points)
            series.Add((t.Ts, t.Value));

        if (series.Count < 10)
            return new MacroResult(Name, "Serie insufficiente.");

        var mean = series.Average(x => x.Value);
        var std = Math.Sqrt(series.Sum(x => Math.Pow(x.Value - mean, 2)) / series.Count);
        var last = series[^1].Value;
        var z = std == 0 ? 0 : (last - mean) / std;
        var msg = z >= 3 ? $"Anomalia: z={z:F2}" : $"OK: z={z:F2}";
        return new MacroResult(Name, msg, new() { ["z"] = z, ["last"] = last, ["mean"] = mean });
    }
}
