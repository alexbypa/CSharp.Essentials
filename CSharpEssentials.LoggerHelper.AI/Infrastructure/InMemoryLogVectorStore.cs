using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;

public sealed class InMemoryLogVectorStore : ILogVectorStore {
    private readonly List<LogEmbedding> _docs;
    private readonly IEmbeddingService _emb;
    public InMemoryLogVectorStore(IEmbeddingService emb, IServiceProvider sp) {
        _emb = emb;
        if (_docs is null) {
            using var scope = sp.CreateScope();
            var fileToLoad = scope.ServiceProvider.GetRequiredService<FileLogIndexer>();
            if (fileToLoad != null) {
                var initialDocs = fileToLoad.IndexStreamAsync(File.OpenRead("C:\\Github\\rag.txt")).Result;
                _docs = initialDocs.ToList();
            }
        }
    }

    public Task UpsertAsync(LogEmbedding doc, CancellationToken ct = default) {
        var i = _docs.FindIndex(d => d.Id == doc.Id);
        if (i >= 0)
            _docs[i] = doc;
        else
            _docs.Add(doc);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LogEmbeddingHit>> SimilarAsync(
        float[] query, int k, string? app = null, TimeSpan? within = null, CancellationToken ct = default) {
        var now = DateTimeOffset.UtcNow;
        var from = within.HasValue ? now - within.Value : DateTimeOffset.MinValue;

        var hits = _docs
            .Where(d => (app is null || d.App == app) && d.Ts >= from)
            .Select(d => new LogEmbeddingHit(d, _emb.Cosine(query, d.Vector)))
            .OrderByDescending(h => h.Score)
            .Take(k)
            .ToList()
            .AsReadOnly();

        return Task.FromResult((IReadOnlyList<LogEmbeddingHit>)hits);
    }
    public void Populate(IEnumerable<LogEmbedding> initialDocs) {
        foreach (var doc in initialDocs) {
            _docs.Add(doc);
        }
    }
}
