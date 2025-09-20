using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Dapper;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
// Simple SQL-backed vector store: persists embeddings as varbinary and
// performs cosine similarity in memory after a coarse SQL filter.
// Replace with a native KNN query if your DB supports it.
public sealed class SqlLogVectorStore : ILogVectorStore {
    //private readonly SqlConnection _db;
    private readonly IWrapperDbConnection _db;
    private readonly IEmbeddingService _emb;

    public SqlLogVectorStore(IWrapperDbConnection db, IEmbeddingService emb) { 
        _db = db; 
        _emb = emb; 
    }

    public async Task UpsertAsync(LogEmbedding doc, CancellationToken ct = default) {
        var bytes = Serialize(doc.Vector);
        var cmd = @"
MERGE dbo.LogVector AS t
USING (SELECT @Id AS Id) AS s
ON (t.Id = s.Id)
WHEN MATCHED THEN UPDATE SET App=@App, Ts=@Ts, Vector=@Vector, Text=@Text, TraceId=@TraceId
WHEN NOT MATCHED THEN INSERT (Id,App,Ts,Vector,Text,TraceId) VALUES (@Id,@App,@Ts,@Vector,@Text,@TraceId);";
        await _db.GetConnection().ExecuteAsync(cmd, new { doc.Id, doc.App, doc.Ts, Vector = bytes, doc.Text, doc.TraceId });
    }

    public async Task<IReadOnlyList<LogEmbeddingHit>> SimilarAsync(float[] query, int k, string? app = null, TimeSpan? within = null, CancellationToken ct = default) {
        var from = within.HasValue ? DateTimeOffset.UtcNow - within.Value : (DateTimeOffset?)null;
        var sql = @"SELECT TOP (@n) Id, App, Ts, Vector, Text, TraceId FROM dbo.LogVector
                    WHERE (@app IS NULL OR App=@app) AND (@from IS NULL OR Ts >= @from)
                    ORDER BY Ts DESC";
        // fetch more than k, then score and take top-k
        var rows = (await _db.GetConnection().QueryAsync(sql, new { n = 200, app, from })).ToList();

        var hits = new List<LogEmbeddingHit>(rows.Count);
        foreach (var r in rows) {
            var vec = Deserialize((byte[])r.Vector);
            var score = _emb.Cosine(query, vec);
            hits.Add(new LogEmbeddingHit(
                new LogEmbedding((string)r.Id, (string)r.App, (DateTimeOffset)r.Ts, vec, (string)r.Text, (string?)r.TraceId),
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