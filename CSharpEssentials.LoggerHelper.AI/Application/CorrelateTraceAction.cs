using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Infrastructure;
using CSharpEssentials.LoggerHelper.AI.Shared;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class CorrelateTraceAction : ILogMacroAction<CorrelateContext> {
    private readonly ISqlQueryWrapper _sqlQueryWrapper;
    public string Name => "CorrelateTrace";
    public Type ContextType => typeof(CorrelateContext);
    private readonly ILlmChat _llm;
    private readonly List<SQLLMModels> _sQLLMModels;
    public CorrelateTraceAction(ISqlQueryWrapper sqlQueryWrapper , ILlmChat llm, List<SQLLMModels> sQLLMModels) {
        _sqlQueryWrapper = sqlQueryWrapper;
        _llm = llm;
        _sQLLMModels = sQLLMModels;
    }
    public bool CanExecute(MacroContextBase ctx) => !string.IsNullOrEmpty(ctx.TraceId);
    public async Task<MacroResult> ExecuteAsync(MacroContextBase ctx, CancellationToken ct = default) {
        var sqlQuery = _sQLLMModels.getQuery(Name, ctx.fileName);
        
        dynamic traceRecords = await _sqlQueryWrapper.QueryAsync(sqlQuery, new {traceid = ctx.TraceId});

        //string Template = "{TraceId} | Span Name={Name} | duration={Duration:F0}ms | tags={TagsJson}";
        var contextBlock =  string.Join("\n---\n", TraceFormatter.FormatRecords(traceRecords, _sQLLMModels.getFieldTemplate(Name, ctx.fileName)));

        var messages = new[]{
            new ChatPromptMessage("system", ctx.system),
            new ChatPromptMessage("assistant", $"CONTEXT:\n{contextBlock}"),
            new ChatPromptMessage("user", $"Question: {ctx.Query}")
        };
        var answer = await _llm.ChatAsync(messages);
        return new MacroResult(Name, answer);
    }
}
