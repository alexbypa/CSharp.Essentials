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
        //TODO:
//        var bytes = Serialize(doc.Vector);
//        var cmd = @"
//MERGE dbo.LogVector AS t
//USING (SELECT @Id AS Id) AS s
//ON (t.Id = s.Id)
//WHEN MATCHED THEN UPDATE SET App=@App, Ts=@Ts, Vector=@Vector, Text=@Text, TraceId=@TraceId
//WHEN NOT MATCHED THEN INSERT (Id,App,Ts,Vector,Text,TraceId) VALUES (@Id,@App,@Ts,@Vector,@Text,@TraceId);";
//        await _db.GetConnection().ExecuteAsync(cmd, new { doc.Id, doc.App, doc.Ts, Vector = bytes, doc.Text, doc.TraceId });
    }

    public async Task<IReadOnlyList<LogEmbeddingHit>> SimilarAsync(float[] query, int k, string? app = null, TimeSpan? within = null, CancellationToken ct = default) {
        var from = within.HasValue ? DateTimeOffset.UtcNow - within.Value : (DateTimeOffset?)null;
        //var sql = @"SELECT TOP (@n) Id, App, Ts, Vector, Text, TraceId FROM dbo.LogVector
        //            WHERE (@app IS NULL OR App=@app) AND (@from IS NULL OR Ts >= @from)
        //            ORDER BY Ts DESC";
        //// fetch more than k, then score and take top-k
        //var rows = (await _db.GetConnection().QueryAsync(sql, new { n = 200, app, from })).ToList();

        var sql = @"select L.""ApplicationName"" ""App"", L.""Level"", L.""TimeStamp"" ""Ts"", L.""Exception"", L.""MachineName"", L.""Action"", L.""Message"", 
	                T.""TraceId"", T.""SpanId"", T.""ParentSpanId"", T.""Name"" spanname, T.""StartTime"", T.""EndTime"", T.""DurationMs"", T.""TagsJson"" tagstraces,
	                M.""Name"" metricname, M.""Value"", M.""TagsJson"" tagsmetrics
	                from public.""LogEntry"" L
		                inner join public.""TraceEntry"" T ON T.""TraceId"" = L.""LogEvent"" ->> 'TraceId'
		                inner join public.""MetricEntry"" M ON T.""TraceId"" = M.""TraceId""
                        --where L.""TimeStamp"" >= '@from'
			                order by L.""Id"" desc
                            LiMIT (@n) 
";
        
        var rows = (await _db.GetConnection().QueryAsync(sql, new { n = 200, app, from })).ToList();

        //var rows = new List<dynamic> {
        //    new {
        //        Id = Guid.NewGuid().ToString(),
        //        App = "myapp",
        //        Ts = DateTimeOffset.UtcNow,
        //        Vector = Serialize(await _emb.EmbedAsync("This is a test log entry.")),
        //        TraceId = Guid.NewGuid().ToString(),
        //        Text = "This is a test log entry."
        //    }
        //};

        var hits = new List<LogEmbeddingHit>(rows.Count);
        foreach (var r in rows) {
            var vec = Deserialize((byte[])Serialize(await _emb.EmbedAsync(r.tagstraces)));
            var score = _emb.Cosine(query, vec);
            hits.Add(new LogEmbeddingHit(
                new LogEmbedding((string)r.Id, (string)r.App, (DateTimeOffset)r.Ts, vec, (string)r.Message, (string?)r.TraceId),
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