using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CSharpEssentials.LoggerHelper.AI;
public sealed class SqlTraceRepository : ITraceRepository {
    readonly SqlConnection _db;
    public SqlTraceRepository(SqlConnection db) => _db = db;

    const string BaseCols = @"
TraceId, SpanId, ParentSpanId, Name,
StartTime, EndTime,
DATEDIFF_BIG(millisecond, StartTime, EndTime) AS DurationMs,
CAST(TagsJson AS nvarchar(max)) AS TagsJson,
CASE WHEN Anomaly IN (1,'1','true') THEN 1 ELSE 0 END AS Anomaly";

    public async Task<TraceRecord?> GetByIdAsync(string traceId) {
        var sql = $"SELECT {BaseCols} FROM dbo.TraceEntry WHERE TraceId=@traceId";
        var r = await _db.QueryFirstOrDefaultAsync(sql, new { traceId });
        return r is null ? null :
            new TraceRecord(r.TraceId, r.SpanId, r.ParentSpanId, r.Name,
                (DateTimeOffset)r.StartTime, (DateTimeOffset)r.EndTime,
                TimeSpan.FromMilliseconds((long)r.DurationMs), r.TagsJson, (bool)r.Anomaly);
    }

    public async Task<IReadOnlyList<TraceRecord>> GetRecentAsync(int limit, CancellationToken ct = default) {
        var sql = $"SELECT TOP (@lim) {BaseCols} FROM dbo.TraceEntry ORDER BY StartTime DESC";
        var rows = await _db.QueryAsync(sql, new { lim = limit });
        return rows.Select(r => new TraceRecord(
            r.TraceId, r.SpanId, r.ParentSpanId, r.Name,
            (DateTimeOffset)r.StartTime, (DateTimeOffset)r.EndTime,
            TimeSpan.FromMilliseconds((long)r.DurationMs), r.TagsJson, (bool)r.Anomaly)).ToList();
    }

    public async Task<IReadOnlyList<TraceRecord>> WithErrorsAsync(int limit) {
        var sql = $"SELECT TOP (@lim) {BaseCols} FROM dbo.TraceEntry WHERE Anomaly = 1 ORDER BY StartTime DESC";
        var rows = await _db.QueryAsync(sql, new { lim = limit });
        return rows.Select(r => new TraceRecord(
            r.TraceId, r.SpanId, r.ParentSpanId, r.Name,
            (DateTimeOffset)r.StartTime, (DateTimeOffset)r.EndTime,
            TimeSpan.FromMilliseconds((long)r.DurationMs), r.TagsJson, (bool)r.Anomaly)).ToList();
    }
}
