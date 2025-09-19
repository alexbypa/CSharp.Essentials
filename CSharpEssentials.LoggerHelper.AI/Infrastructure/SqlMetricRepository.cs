using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
public sealed class SqlMetricRepository : IMetricRepository {
    readonly SqlConnection _db;
    public SqlMetricRepository(SqlConnection db) => _db = db;

    public async Task<IReadOnlyList<MetricPoint>> QueryAsync(string name, DateTimeOffset from, DateTimeOffset to) {
        var sql = @"
SELECT TOP (200) 
    Name, 
    CAST(Value AS float) AS Value, 
    CAST([Timestamp] AS datetimeoffset) AS Timestamp,
    CAST(TagsJson AS nvarchar(max)) AS TagsJson, TraceId
FROM dbo.MetricEntry
WHERE Name=@name --AND [Timestamp] BETWEEN @from AND @to
ORDER BY [Timestamp]";
        var rows = await _db.QueryAsync<MetricPoint>(sql, new { name, from, to });
        return rows.AsList();
    }

    public async Task<MetricPoint?> LastAsync(string name) {
        var sql = @"
SELECT TOP 1 Name, CAST(Value AS float) AS Value, [Timestamp] AS Ts,
CAST(TagsJson AS nvarchar(max)) AS TagsJson, TraceId
FROM dbo.MetricEntry
WHERE Name=@name
ORDER BY [Timestamp] DESC";
        return await _db.QueryFirstOrDefaultAsync<MetricPoint>(sql, new { name });
    }
}
