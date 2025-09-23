namespace CSharpEssentials.LoggerHelper.AI.Domain;
// Vector-store contracts for storing and querying log embeddings.
// Replace this with a native vector DB if available (pgvector, Qdrant, etc.).
public interface ILogVectorStore {
    Task UpsertAsync(LogEmbedding doc, CancellationToken ct = default);
    Task<IReadOnlyList<LogEmbeddingHit>> SimilarAsync(string sqlQuery, float[] query, int k, string? app = null, TimeSpan? within = null, CancellationToken ct = default);
}
public sealed record LogEmbedding(
    string Id,               // unique id, e.g. $"{traceId}:{logId}"
    string App,              // app/service name
    DateTimeOffset Ts,       // log timestamp
    float[] Vector,          // embedding vector
    string Text,             // normalized log text
    string? TraceId          // optional correlation id
);

public sealed record LogEmbeddingHit(LogEmbedding Doc, double Score); // cosine similarity