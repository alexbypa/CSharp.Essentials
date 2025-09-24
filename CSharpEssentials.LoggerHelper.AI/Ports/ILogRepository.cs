using CSharpEssentials.LoggerHelper.AI.Domain;

namespace CSharpEssentials.LoggerHelper.AI.Ports;
public interface ILogRepository {
    Task<IReadOnlyList<LogRecord>> GetRecentAsync(string? app, int limit);
    Task<IReadOnlyList<LogRecord>> SearchAsync(string term, int limit);
    Task<IReadOnlyList<LogRecord>> ByTraceAsync(string traceId, int limit);
}

public interface ITraceRepository<TraceRecord> {
    Task<List<TraceRecord>> GetByTraceIdAsync(string sqlQuery, string traceId);
    Task<IReadOnlyList<TraceRecord>> GetRecentAsync(int limit, CancellationToken ct = default);
    Task<IReadOnlyList<TraceRecord>> WithErrorsAsync(int limit);
}

public interface IMetricRepository {
    Task<IReadOnlyList<MetricPoint>> QueryAsync(string name, DateTimeOffset from, DateTimeOffset to);
    Task<MetricPoint?> LastAsync(string name);
}
