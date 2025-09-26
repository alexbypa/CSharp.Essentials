using CSharpEssentials.LoggerHelper.AI.Ports;
using Dapper;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
public interface ISqlQueryWrapper {
    Task<List<object>> QueryAsync(string sql, object? param = null);
}
public class SqlQueryWrapper : ISqlQueryWrapper {
    private readonly FactorySQlConnection _db;
    public SqlQueryWrapper(FactorySQlConnection db) => _db = db;

    public async Task<List<object>> QueryAsync(string sql, object? param = null) {
        using var connection = _db.GetConnection();
        var queryResult = await connection.QueryAsync(sql, param);
        return queryResult.AsList();
    }
}