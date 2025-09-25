//using CSharpEssentials.LoggerHelper.AI.Ports;

//namespace CSharpEssentials.LoggerHelper.AI.Domain;
//// Batch indexer: converts recent logs to embeddings and stores them.
//// Call this on a schedule or after ingestion.
//public sealed class LogIndexer {
//    //private readonly ILogRepository _logs;
//    private readonly IEmbeddingService _emb;
//    private readonly ILogVectorStore _store;

//    public LogIndexer(ILogRepository logs, IEmbeddingService emb, ILogVectorStore store) {
//        _logs = logs;
//        _emb = emb;
//        _store = store;
//    }

//    public async Task IndexRecentAsync(string? app, int limit, CancellationToken ct = default) {
//        var items = await _logs.GetRecentAsync(app, limit);
//        foreach (var r in items) {
//            var text = Normalize($"{r.Level} {r.Message} {r.Exception}");
//            var vec = await _emb.EmbedAsync(text);
//            var id = $"{r.IdTransaction}:{r.Id}";
//            await _store.UpsertAsync(new LogEmbedding(id, r.ApplicationName ?? "", r.TimeStamp, vec, text, r.IdTransaction), ct);
//        }
//    }

//    private static string Normalize(string s) =>
//        s.Replace("\r", " ").Replace("\n", " ").Trim();
//}