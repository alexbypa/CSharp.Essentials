using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Microsoft.Data.SqlClient;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure {
    public static class SqlAiPersistenceFactory {
        public static Action<IServiceCollection> AddSqlAiPersistence(IConfiguration configuration) {
            return services => {
                var connectionString = configuration.GetConnectionString("Default");
                var databaseProvider = configuration.GetValue<string>("DatabaseProvider");

                if (databaseProvider != null && databaseProvider.Contains("postgresql", StringComparison.InvariantCultureIgnoreCase)) {
                    services.AddScoped(_ => new NpgsqlConnection(connectionString));
                    services.AddScoped<IWrapperDbConnection>(_ => new FactoryPostgreSqlConnection(connectionString!));
                } else {
                    services.AddScoped(_ => new SqlConnection(connectionString));
                    services.AddScoped<IWrapperDbConnection>(_ => new FactorySQlConnection(connectionString!));
                }
                //services.AddScoped<ILogVectorStore, SqlLogVectorStore>();
            };
        }
    }
}