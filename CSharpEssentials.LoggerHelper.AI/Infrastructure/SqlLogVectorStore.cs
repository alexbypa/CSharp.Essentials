using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Dapper;
using Microsoft.IdentityModel.Abstractions;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
// Simple SQL-backed vector store: persists embeddings as varbinary and
// performs cosine similarity in memory after a coarse SQL filter.
// Replace with a native KNN query if your DB supports it.
public sealed class SqlLogVectorStore : ILogVectorStore {
    //private readonly SqlConnection _db;
    private readonly IWrapperDbConnection _db;
    private readonly IEmbeddingService _emb;
    //private readonly IFileLoader _fileLoader;
    public SqlLogVectorStore(IWrapperDbConnection db, IEmbeddingService emb/*, IFileLoader fileLoader*/) { 
        _db = db; 
        _emb = emb;
        //_fileLoader = fileLoader;
    }

    public async Task UpsertAsync(LogEmbedding doc, CancellationToken ct = default) {
        //TODO:....
    }

    public async Task<IReadOnlyList<LogEmbeddingHit>> SimilarAsync(string sqlQuery, float[] query, int k, DateTimeOffset from, CancellationToken ct = default) {
        //var from = within.HasValue ? DateTimeOffset.UtcNow - within.Value : (DateTimeOffset?)null;
        //var sql = _fileLoader.getModelSQLLMModels();

        var rows = (await _db.GetConnection().QueryAsync(sqlQuery, new { n = 200, now = from })).ToList();

        var hits = new List<LogEmbeddingHit>(rows.Count);
        foreach (var r in rows) {
            var vec = Deserialize((byte[])Serialize(await _emb.EmbedAsync(r.Message)));
            var score = _emb.Cosine(query, vec);
            hits.Add(new LogEmbeddingHit(
                new LogEmbedding(r.Id.ToString(), (string)r.App, (DateTimeOffset)r.Ts, vec, (string)r.Message, (string?)r.TraceId),
                score));
        }
        return hits.OrderByDescending(h => h.Score).Take(k).ToList();
    }

    static byte[] Serialize(float[] v) {
        var bytes = new byte[v.Length * sizeof(float)];
        Buffer.BlockCopy(v, 0, bytes, 0, bytes.Length);
        return bytes;
    }
    static float[] Deserialize(byte[] bytes) {
        var v = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, v, 0, bytes.Length);
        return v;
    }
}