using CSharpEssentials.LoggerHelper.AI.Ports;
using Dapper;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure;
public interface ISqlQueryWrapper {
    Task<List<object>> QueryAsync(string sql, object? param = null);
}
public class SqlQueryWrapper : ISqlQueryWrapper {
    private readonly IWrapperDbConnection _db;
    public SqlQueryWrapper(IWrapperDbConnection db) => _db = db;
    public async Task<List<object>> QueryAsync(string sql, object? param = null) {
        var Query = await _db.GetConnection().QueryAsync(sql, param);
        return Query.AsList();
    }
}