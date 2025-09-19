namespace CSharpEssentials.LoggerHelper.AI.Domain;
public sealed record LogRecord(
    int Id, DateTime TimeStamp, string Level, string Message,
    string? Exception, string? IdTransaction, string? MachineName, string? ApplicationName);

public sealed record TraceRecord(
    string TraceId, string SpanId, string? ParentSpanId, string Name,
    DateTimeOffset StartTime, DateTimeOffset EndTime, TimeSpan Duration, string? TagsJson, bool? Anomaly);

public sealed record MetricPoint(
    string Name, double Value, DateTimeOffset TimeStamp, string? TagsJson, string? TraceId);
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
    Task<string> ChatAsync(string system, string user, double temperature = 0.0);
}
// Domain/LlmOptions.cs
public sealed class LlmOptions {
        public string Model { get; set; } = "gpt-4o-mini";
        public double DefaultTemperature { get; set; } = 0.2;
}
// Macro Action (OCP + DIP)
public interface ILogMacroAction {
    string Name { get; }                 // es. "SummarizeIncident"
    bool CanExecute(MacroContext ctx);   // regole veloci
    Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default);
}

public sealed record MacroContext(string? DocId, string? TraceId, string? Query, DateTimeOffset Now);
public sealed record MacroResult(string Action, string Summary, Dictionary<string, object>? Data = null);