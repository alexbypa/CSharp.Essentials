using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class DetectAnomalyAction : ILogMacroAction<DetectAnomalyContext> {
    private readonly IMetricRepository _metrics;
    public string Name => "DetectAnomaly";

    public Type ContextType => typeof(DetectAnomalyContext);

    private readonly List<SQLLMModels> _sQLLMModels;
    private readonly ILlmChat _llm;
    public DetectAnomalyAction(IMetricRepository m, List<SQLLMModels> sQLLMModels, ILlmChat llm) => (_metrics, _sQLLMModels, _llm) = (m, sQLLMModels, llm);
    public bool CanExecute(IMacroContext ctx) => true;
    public async Task<MacroResult> ExecuteAsync(IMacroContext ctx, CancellationToken ct = default) {
        var to = ctx.dtStart;
        var from = to.AddMinutes(-30);
        var series = new List<(DateTimeOffset Time, double Value)>();

        var sqlQuery = _sQLLMModels.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == ctx.fileName)?.content;
        
        var metrics = await _metrics.QueryAsync(sqlQuery, from, to);

        var hits = metrics.Select(a => new {
            Id = a.TraceId,
            Message = a.TagsJson,
            //trace = a.TraceJson,
            Score = a.Value
        }).ToList();

        var contextData = string.Join("\n", hits.Select(h => $"TraceId: {h.Id} | LogEvent: {h.Message} | Score: {h.Score}"));
        // altrimenti: var points = await _metrics.QueryAsync("http_5xx_rate", from, to);

        var contextBlock = string.Join("\n---\n", hits.Select(h => h.Message));
        var messages = new[]
        {
            new ChatPromptMessage("system", ctx.system),
            new ChatPromptMessage("assistant", $"CONTEXT:\n{contextBlock}"),
            new ChatPromptMessage("user", $"Question: {ctx.Query}")
        };

        // 4) Generazione
        var answer = await _llm.ChatAsync(messages);

        return new MacroResult(Name, answer);

    }
}
