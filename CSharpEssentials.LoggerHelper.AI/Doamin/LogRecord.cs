namespace CSharpEssentials.LoggerHelper.AI.Doamin;
public sealed record LogRecord(
    long Id, DateTimeOffset Ts, string Level, string Message,
    string? Exception, string? TraceId, string? Machine, string? App);

public sealed record TraceRecord(
    string TraceId, string SpanId, string? ParentSpanId, string Name,
    DateTimeOffset StartTime, DateTimeOffset EndTime, TimeSpan Duration, string? TagsJson, bool? Anomaly);

public sealed record MetricPoint(
    string Name, double Value, DateTimeOffset Ts, string? TagsJson, string? TraceId);

public interface IEmbeddingService {
    Task<float[]> EmbedAsync(string text);
    double Cosine(float[] a, float[] b);
}

public interface ILlmChat {
    Task<string> ChatAsync(string system, string user, double temperature = 0.0);
}

// Macro Action (OCP + DIP)
public interface ILogMacroAction {
    string Name { get; }                 // es. "SummarizeIncident"
    bool CanExecute(MacroContext ctx);   // regole veloci
    Task<MacroResult> ExecuteAsync(MacroContext ctx, CancellationToken ct = default);
}

public sealed record MacroContext(string? DocId, string? TraceId, string? Query, DateTimeOffset Now);
public sealed record MacroResult(string Action, string Summary, Dictionary<string, object>? Data = null);