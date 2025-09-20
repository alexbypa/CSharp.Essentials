using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Microsoft.Data.SqlClient;
using Dapper;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;

public sealed class SqlLogRepository : ILogRepository {
    readonly IWrapperDbConnection _db;
    public SqlLogRepository(IWrapperDbConnection db) => _db = db;

    public async Task<IReadOnlyList<LogRecord>> GetRecentAsync(string? app, int limit) {
        var sql = @"
SELECT TOP (@lim)
    Id, CAST(JSON_VALUE(LogEvent,'$.Timestamp') AS datetimeoffset) AS Ts,
    Level, Message, Exception,
    JSON_VALUE(LogEvent,'$.TraceId') AS TraceId,
    MachineName AS Machine, ApplicationName AS App
FROM dbo.LogEntry
WHERE (@app IS NULL OR ApplicationName = @app)
ORDER BY Ts DESC";
        var rows = await _db.GetConnection().QueryAsync<LogRecord>(sql, new { lim = limit, app });
        return rows.AsList();
    }

    public async Task<IReadOnlyList<LogRecord>> SearchAsync(string term, int limit) {
        var sql = @"
SELECT TOP (@lim)
    Id, CAST(JSON_VALUE(LogEvent,'$.Timestamp') AS datetimeoffset) AS Ts,
    Level, Message, Exception,
    JSON_VALUE(LogEvent,'$.TraceId') AS TraceId,
    MachineName AS Machine, ApplicationName AS App
FROM dbo.LogEntry
WHERE Message LIKE '%' + @q + '%' OR RenderedMessage LIKE '%' + @q + '%'
ORDER BY Ts DESC";
        var rows = await _db.GetConnection().QueryAsync<LogRecord>(sql, new { lim = limit, q = term });
        return rows.AsList();
    }

    public async Task<IReadOnlyList<LogRecord>> ByTraceAsync(string traceId, int limit) {
        var sql = @"
SELECT TOP (@lim)
    Id, TimeStamp,
    Level, Message, Exception,
    @traceId AS IdTransaction, MachineName, ApplicationName 
FROM dbo.LogEntry
--WHERE JSON_VALUE(LogEvent,'$.TraceId') = @traceId
WHERE IdTransaction = @traceId
ORDER BY TimeStamp DESC";
        var rows = await _db.GetConnection().QueryAsync<LogRecord>(sql, new { lim = limit, traceId });
        return rows.AsList();
    }
}
