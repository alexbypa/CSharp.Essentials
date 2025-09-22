using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.Extensions.Options;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class RagAnswerQueryAction : ILogMacroAction {
    private readonly IEmbeddingService _emb;
    private readonly ILogVectorStore _store;
    private readonly ILlmChat _llm;
    private readonly IOptions<LoggerAIOptions> _opt;
    public string Name => "RagAnswerQuery";
    public RagAnswerQueryAction(IEmbeddingService emb, ILogVectorStore store, ILlmChat llm, IOptions<LoggerAIOptions> opt)
        => (_emb, _store, _llm, _opt) = (emb, store, llm, opt);

    public bool CanExecute(MacroContext ctx) => !string.IsNullOrWhiteSpace(ctx.Query);

    public async Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default) {
        // 1) Embed query
        var qvec = await _emb.EmbedAsync(ctx.Query!);
        


        // 2) Top-K documenti simili dal vector store (ultime 24h)
        var hits = await _store.SimilarAsync(qvec, k: _opt.Value.topScore, app: null, within: TimeSpan.FromHours(24), ct);

        // 3) Prompt con contesto recuperato + domanda utente
        var system = "You are an SRE assistant. Use the provided CONTEXT to answer precisely and concisely.";
        //var system = "Rispondi alla domanda dell'utente basandoti esclusivamente sul seguente contesto. Se le informazioni non sono presenti nel contesto, rispondi 'Non ho abbastanza informazioni per rispondere'.";
        if (ctx.system is not null)
            system = ctx.system;

        var contextBlock = string.Join("\n---\n", hits.Select(h => h.Doc.Text));
        var messages = new[]
        {
            new ChatPromptMessage("system", system),
            new ChatPromptMessage("assistant", $"CONTEXT:\n{contextBlock}"),
            new ChatPromptMessage("user", $"Question: {ctx.Query}")
        };

        // 4) Generazione
        var answer = await _llm.ChatAsync(messages);

        return new MacroResult(Name, answer, new() {
            ["matches"] = hits.Count,
            //["topScore"] = hits.FirstOrDefault()?.Score ?? 0,
            ["topScore"] = hits.ToList()
        });
    }
}

