using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.Extensions.Options;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class RagAnswerQueryAction : ILogMacroAction<RagContext> {
    private readonly IEmbeddingService _emb;
    private readonly ILogVectorStore _store;
    private readonly ILlmChat _llm;
    private readonly IOptions<LoggerAIOptions> _opt;
    private readonly List<SQLLMModels> _sQLLMModels;
    public string Name => "RagAnswerQuery";

    public Type ContextType => typeof(RagContext);

    public RagAnswerQueryAction(IEmbeddingService emb, ILogVectorStore store, ILlmChat llm, IOptions<LoggerAIOptions> opt, List<SQLLMModels> sQLLMModels)
        => (_emb, _store, _llm, _opt, _sQLLMModels) = (emb, store, llm, opt, sQLLMModels);

    
    public bool CanExecute(MacroContextBase ctx) => !string.IsNullOrWhiteSpace(ctx.Query);

    public async Task<MacroResult> ExecuteAsync(MacroContextBase ctx, CancellationToken ct = default) {
        var qvec = await _emb.EmbedAsync(ctx.Query!);

        var sqlQuery = _sQLLMModels.getQuery(Name, ctx.fileName);
        var hits = await _store.SimilarAsync(sqlQuery, qvec, k: ctx.topResultsOnQuery, ctx.dtStart.ToUniversalTime(), ct);
        
        var contextBlock = string.Join("\n---\n", hits.Select(h => h.Doc.Text));
        var messages = new[]
        {
            new ChatPromptMessage("system", ctx.system),
            new ChatPromptMessage("assistant", $"CONTEXT:\n{contextBlock}"),
            new ChatPromptMessage("user", $"Question: {ctx.Query}")
        };

        var answer = await _llm.ChatAsync(messages);

        return new MacroResult(Name, answer, new() {
            ["matches"] = hits.Count,
            ["topScore"] = hits.ToList()
        });
    }
}

