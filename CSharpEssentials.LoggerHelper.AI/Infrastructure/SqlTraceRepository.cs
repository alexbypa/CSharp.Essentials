using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Dapper;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
public sealed class SqlTraceRepository : ITraceRepository<TraceRecord> {
    readonly IWrapperDbConnection _db;
    public SqlTraceRepository(IWrapperDbConnection db) => _db = db;

    const string BaseCols = @"
TraceId, SpanId, ParentSpanId, Name,
StartTime, EndTime,
DATEDIFF_BIG(millisecond, StartTime, EndTime) AS DurationMs,
CAST(TagsJson AS nvarchar(max)) AS TagsJson,
CASE WHEN Anomaly IN ('true') THEN convert(bit, 1) ELSE convert(bit, 0) END AS Anomaly";

    public async Task<List<TraceRecord>> GetByTraceIdAsync(string sqlQuery, string traceId) {
        //var sql = $"SELECT {BaseCols} FROM dbo.TraceEntry WHERE TraceId=@traceId";
        var result = await _db.GetConnection().QueryAsync<TraceRecord>(sqlQuery, new { traceId });
        return result.ToList();
        //return r is null ? null :
        //    new TraceRecord(r.TraceId, r.SpanId, r.ParentSpanId, r.Name,
        //        (DateTimeOffset)r.StartTime, (DateTimeOffset)r.EndTime,
        //        TimeSpan.FromMilliseconds((long)r.DurationMs), r.TagsJson, (bool)r.Anomaly);
    }

    public async Task<IReadOnlyList<TraceRecord>> GetRecentAsync(int limit, CancellationToken ct = default) {
        //TODO:
        throw new NotImplementedException();
        //var sql = $"SELECT TOP (@lim) {BaseCols} FROM dbo.TraceEntry ORDER BY StartTime DESC";
        //var rows = await _db.GetConnection().QueryAsync(sql, new { lim = limit });
        //return rows.Select(r => new TraceRecord(
        //    r.TraceId, r.SpanId, r.ParentSpanId, r.Name,
        //    (DateTimeOffset)r.StartTime, (DateTimeOffset)r.EndTime,
        //    TimeSpan.FromMilliseconds((long)r.DurationMs), r.TagsJson, (bool)r.Anomaly)).ToList();
    }

    public async Task<IReadOnlyList<TraceRecord>> WithErrorsAsync(int limit) {
        //TODO:
        throw new NotImplementedException();
        //var sql = $"SELECT TOP (@lim) {BaseCols} FROM dbo.TraceEntry WHERE Anomaly = 1 ORDER BY StartTime DESC";
        //var rows = await _db.GetConnection().QueryAsync(sql, new { lim = limit });
        //return rows.Select(r => new TraceRecord(
        //    r.TraceId, r.SpanId, r.ParentSpanId, r.Name,
        //    (DateTimeOffset)r.StartTime, (DateTimeOffset)r.EndTime,
        //    TimeSpan.FromMilliseconds((long)r.DurationMs), r.TagsJson, (bool)r.Anomaly)).ToList();
    }

    public Task<List<TraceRecord>> GetByTraceIdAsync(string traceId) {
        throw new NotImplementedException();
    }
}