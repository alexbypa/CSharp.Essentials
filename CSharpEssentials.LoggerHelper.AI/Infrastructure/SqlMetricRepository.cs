using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Dapper;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
public sealed class SqlMetricRepository /*: IMetricRepository*/ {
    readonly IWrapperDbConnection _db;
    public SqlMetricRepository(IWrapperDbConnection db) => _db = db;

    public async Task<IReadOnlyList<MetricPoint>> QueryAsync(string sqlQuery, DateTimeOffset from, DateTimeOffset to) {
        var rows = await _db.GetConnection().QueryAsync<MetricPoint>(sqlQuery);
        return rows.AsList();
        //        var sql = @"
        //SELECT TOP (200) 
        //    Name, 
        //    CAST(Value AS float) AS Value, 
        //    CAST([Timestamp] AS datetimeoffset) AS Timestamp,
        //    CAST(TagsJson AS nvarchar(max)) AS TagsJson, TraceId
        //FROM dbo.MetricEntry
        //WHERE Name=@name --AND [Timestamp] BETWEEN @from AND @to
        //ORDER BY [Timestamp]";
        //        var rows = await _db.GetConnection().QueryAsync<MetricPoint>(sql, new { name, from, to });
        //        return rows.AsList();
    }

    public async Task<MetricPoint?> LastAsync(string name) {
        throw new NotImplementedException();
        //        var sql = @"
        //SELECT TOP 1 Name, CAST(Value AS float) AS Value, [Timestamp] AS Ts,
        //CAST(TagsJson AS nvarchar(max)) AS TagsJson, TraceId
        //FROM dbo.MetricEntry
        //WHERE Name=@name
        //ORDER BY [Timestamp] DESC";
        //        return await _db.GetConnection().QueryFirstOrDefaultAsync<MetricPoint>(sql, new { name });
    }
}
