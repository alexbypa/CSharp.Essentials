using Microsoft.Data.SqlClient;
using Npgsql;

namespace CSharpEssentials.LoggerHelper.AI.Ports;
public interface IWrapperDbConnection {
    System.Data.IDbConnection GetConnection();
}
public sealed class FactoryPostgreSqlConnection : IWrapperDbConnection {
    private readonly string _ConnectionString;
    public FactoryPostgreSqlConnection(string ConnectionString) {
        _ConnectionString = ConnectionString;
    }
    public System.Data.IDbConnection GetConnection() {
        return new NpgsqlConnection(_ConnectionString);
    }
}
public sealed class FactorySQlConnection : IWrapperDbConnection {
    private readonly string _ConnectionString;
    public FactorySQlConnection(string ConnectionString) {
        _ConnectionString = ConnectionString;
    }
    public System.Data.IDbConnection GetConnection() {
        return new SqlConnection(_ConnectionString);
    }
}