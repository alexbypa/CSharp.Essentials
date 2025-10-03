namespace CSharpEssentials.LoggerHelper.AI.Domain;
public sealed record LogRecord(
    int Id, DateTime TimeStamp, string Level, string Message,
    string? Exception, string? IdTransaction, string? MachineName, string? ApplicationName);
public sealed class TraceRecord {
    public TraceRecord() { }

    public string TraceId { get; set; }
    public string SpanId { get; set; }
    public string? ParentSpanId { get; set; }
    public string Name { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public double Duration { get; set; }
    public string? TagsJson { get; set; }
}
public sealed class MetricPoint {
    public MetricPoint() {
        
    }
    public string Name { get; set; }
    public double Value { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public string? TagsJson { get; set; }
    public string? TraceId { get; set; }
}
/// <summary>
/// Contract for text embeddings and cosine similarity.
/// Use a remote API (e.g., OpenAI /embeddings) or a local model.
/// RAG (Retrieval Augmented Generation)
///     Retrieval = recupero dei dati simili.
///     Augmented = aggiunti al prompt.
///     Generation = il modello produce la risposta finale.
/// </summary>ILogVectorStore 
public interface IEmbeddingService {
    Task<float[]> EmbedAsync(string text);
    double Cosine(float[] a, float[] b);
}

public interface ILlmChat {
    /// <summary>
    /// Sends a sequence of chat messages to the model.  Each message should specify a role
    /// ("system", "user" or "assistant") and contents.  This overload can be used to include
    /// few‑shot examples or additional context before the final user prompt.
    /// </summary>
    Task<string> ChatAsync(IEnumerable<ChatPromptMessage> messages);
}
public sealed record MacroResult(string Action, string Summary, Dictionary<string, object>? Data = null);