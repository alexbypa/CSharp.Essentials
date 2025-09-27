using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Infrastructure;
using CSharpEssentials.LoggerHelper.AI.Shared;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class DetectAnomalyAction : ILogMacroAction<DetectAnomalyContext> {
    public string Name => "DetectAnomaly";
    private readonly ISqlQueryWrapper _sqlQueryWrapper;
    public Type ContextType => typeof(DetectAnomalyContext);

    private readonly List<SQLLMModels> _sQLLMModels;
    private readonly ILlmChat _llm;
    public DetectAnomalyAction(ISqlQueryWrapper sqlQueryWrapper, List<SQLLMModels> sQLLMModels, ILlmChat llm) => (_sqlQueryWrapper, _sQLLMModels, _llm) = (sqlQueryWrapper, sQLLMModels, llm);
    public bool CanExecute(MacroContextBase ctx) => true;
    public async Task<MacroResult> ExecuteAsync(MacroContextBase ctx, CancellationToken ct = default) {
        var sqlQuery = _sQLLMModels.getQuery(Name, ctx.fileName);
        dynamic traceRecords = await _sqlQueryWrapper.QueryAsync(sqlQuery, new {from = ctx.dtStart, to = ctx.dtEnd});

        //Context example   : You are a systems analyst with expertise in observability. Analyze the provided metrics and logs (CONTEXT) to identify the root cause of the detected anomaly and recommend mitigation. Prioritize abnormal error rates and latency.
        //user example      : Analyze the data in context. Is there an anomaly? If so, what is the root cause and possible solution?
        //string Template = "TraceId: {Id} | LogEvent: {TgasJson} | Score: {Value}";
        var contextBlock = string.Join("\n---\n", TraceFormatter.FormatRecords(traceRecords, _sQLLMModels.getFieldTemplate(Name, ctx.fileName)));
        

        var messages = new[]
        {
            new ChatPromptMessage("system", ctx.system),
            new ChatPromptMessage("assistant", $"CONTEXT:\n{contextBlock}"),
            new ChatPromptMessage("user", $"Question: {ctx.Query}")
        };

        var answer = await _llm.ChatAsync(messages);

        return new MacroResult(Name, answer);

    }
}
