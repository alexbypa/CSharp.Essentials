using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;

/// <summary>
/// // Questa classe è un'implementazione concreta dell'interfaccia 'ILogVectorStore'.
/// Rappresenta il nostro "Magazzino Dati" in-memory.
/// Le sue responsabilità sono:
/// 1. CONSERVARE una collezione di 'LogEmbedding'.
/// 2. FORNIRE metodi per aggiungere/aggiornare dati (UpsertAsync).
/// 3. FORNIRE metodi per cercare dati in base alla similarità semantica (SimilarAsync).
///
/// Grazie all'astrazione (l'interfaccia), potremmo sostituire questa classe con un
/// 'SqlLogVectorStore' o 'RedisLogVectorStore' senza modificare il resto dell'applicazione.
/// </summary>
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
    // NOTA: L'implementazione attuale non gestisce l'aggiornamento (Update),
    // ma solo l'inserimento (Insert). Una logica di 'Upsert' completa
    // dovrebbe prima cercare se un documento con lo stesso ID esiste e, in caso,
    // sostituirlo. Per ora, aggiunge semplicemente il nuovo documento.
    public Task UpsertAsync(LogEmbedding doc, CancellationToken ct = default) {
        var i = _docs.FindIndex(d => d.Id == doc.Id);
        if (i >= 0)
            _docs[i] = doc;
        else
            _docs.Add(doc);
        return Task.CompletedTask;
    }
    // --- FLUSSO DI RICERCA SEMANTICA (CORE LOGIC) ---
    public Task<IReadOnlyList<LogEmbeddingHit>> SimilarAsync(string sqlQuery, float[] query, int k, DateTimeOffset from, CancellationToken ct = default) {
        var now = DateTimeOffset.UtcNow;
        //var from = within.HasValue ? now - within.Value : DateTimeOffset.MinValue;

        var hits = _docs
            .Where(d => d.Ts >= from)
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