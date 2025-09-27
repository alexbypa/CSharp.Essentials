using CSharpEssentials.LoggerHelper.AI.Ports;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure {
    public static class SqlAiPersistenceFactory {
        public static Action<IServiceCollection> AddSqlAiPersistence(IConfiguration configuration) {
            return services => {
                services.AddScoped<ISqlQueryWrapper, SqlQueryWrapper>();

                var connectionString = configuration.GetConnectionString("Default");
                var databaseProvider = configuration.GetValue<string>("DatabaseProvider");

                if (databaseProvider != null && databaseProvider.Contains("postgresql", StringComparison.InvariantCultureIgnoreCase)) {
                    services.AddScoped(_ => new NpgsqlConnection(connectionString));
                    services.AddScoped<IWrapperDbConnection>(_ => new FactoryPostgreSqlConnection(connectionString!));
                } else {
                    services.AddScoped(_ => new SqlConnection(connectionString));
                    services.AddScoped<IWrapperDbConnection>(_ => new FactorySQlConnection(connectionString!));
                }
            };
        }
    }
}