using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Infrastructure;
using CSharpEssentials.LoggerHelper.AI.Ports;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class DetectAnomalyAction : ILogMacroAction<DetectAnomalyContext> {
    public string Name => "DetectAnomaly";
    private readonly ISqlQueryWrapper _sqlQueryWrapper;
    public Type ContextType => typeof(DetectAnomalyContext);

    private readonly List<SQLLMModels> _sQLLMModels;
    private readonly ILlmChat _llm;
    public DetectAnomalyAction(ISqlQueryWrapper sqlQueryWrapper, List<SQLLMModels> sQLLMModels, ILlmChat llm) => (_sqlQueryWrapper, _sQLLMModels, _llm) = (sqlQueryWrapper, sQLLMModels, llm);
    public bool CanExecute(IMacroContext ctx) => true;
    public async Task<MacroResult> ExecuteAsync(IMacroContext ctx, CancellationToken ct = default) {
        var to = ctx.dtStart;
        var from = to.AddMinutes(-30);
        var series = new List<(DateTimeOffset Time, double Value)>();

        var sqlQuery = _sQLLMModels.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == ctx.fileName)?.content;
        
        dynamic traceRecords = await _sqlQueryWrapper.QueryAsync(sqlQuery, ctx.TraceId);
        //var metrics = await _metrics.QueryAsync(sqlQuery, from, to);


        //TraceFormatterService _formatter;
        //string myFormatTemplate = "TraceId: {h.Id} | LogEvent: {h.Message} | Score: {h.Score}";
        //_formatter = new TraceFormatterService(myFormatTemplate);
        //var contextBlock = _formatter.Format(traceRecords);

        string Template = "TraceId: {h.Id} | LogEvent: {h.Message} | Score: {h.Score}";
        var contextBlock = string.Join("\n---\n", TraceFormatter.FormatRecords(traceRecords, Template));



        //var hits = metrics.Select(a => new {
        //    Id = a.TraceId,
        //    Message = a.TagsJson,
        //    //trace = a.TraceJson,
        //    Score = a.Value
        //}).ToList();

        //var contextData = string.Join("\n", hits.Select(h => $"TraceId: {h.Id} | LogEvent: {h.Message} | Score: {h.Score}"));
        // altrimenti: var points = await _metrics.QueryAsync("http_5xx_rate", from, to);

        //var contextBlock = string.Join("\n---\n", hits.Select(h => h.Message));
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
