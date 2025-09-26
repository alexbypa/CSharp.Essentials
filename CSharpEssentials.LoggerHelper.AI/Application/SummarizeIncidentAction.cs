using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Infrastructure;
using CSharpEssentials.LoggerHelper.AI.Shared;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public sealed class SummarizeIncidentAction : ILogMacroAction<SummarizeContext> {
    //private readonly ILogRepository _logs; 
    private readonly ILlmChat _llm;
    private readonly ISqlQueryWrapper _sqlQueryWrapper;
    public string Name => "SummarizeIncident";
    public Type ContextType => typeof(SummarizeContext);
    private readonly List<SQLLMModels> _sQLLMModels;
    public SummarizeIncidentAction(ISqlQueryWrapper sqlQueryWrapper, ILlmChat llm, List<SQLLMModels> sQLLMModels) {
        sqlQueryWrapper = _sqlQueryWrapper;
        _llm = llm;
        _sQLLMModels = sQLLMModels;
    }
    public bool CanExecute(MacroContextBase ctx) => !string.IsNullOrEmpty(ctx.TraceId);
    public async Task<MacroResult> ExecuteAsync(MacroContextBase ctx, CancellationToken ct = default) {
        // 1) Fetch logs for the given trace
        // Use the dedicated API for correlation by TraceId
        var records = await _sqlQueryWrapper.QueryAsync(ctx.TraceId!, 200);

        //var lines = records.Select(x => $"{x.TimeStamp:u} {x.Level} {Compact(x.Message)}").ToList();
        var contextBlock = string.Join("\n---\n", TraceFormatter.FormatRecords(records, _sQLLMModels.getFieldTemplate(Name, ctx.fileName)));

        var messages = new List<ChatPromptMessage>
        {
            new("system", ctx.system),
            new("assistant", $"CONTEXT:\n{contextBlock}"),
            new("user",      $"Question: {ctx.Query}")
        };

        // 5) Call the LLM
        var summary = await _llm.ChatAsync(messages);

        return new MacroResult(Name, summary);
    }
}