using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using Microsoft.Data.SqlClient;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure {
    public static class SqlAiPersistenceFactory {
        // Questo metodo NON è un Extension Method su IServiceCollection.
        // È una 'fabbrica' che restituisce l'Action<IServiceCollection> che farà il lavoro.
        public static Action<IServiceCollection> AddSqlAiPersistence(IConfiguration configuration) {
            return services => {
                // La logica di persistenza è totalmente isolata qui.
                var connectionString = configuration.GetConnectionString("Default");
                var databaseProvider = configuration.GetValue<string>("DatabaseProvider");

                // SEZIONE DATABASE (SRP)
                if (databaseProvider != null && databaseProvider.Contains("postgresql", StringComparison.InvariantCultureIgnoreCase)) {
                    services.AddScoped(_ => new NpgsqlConnection(connectionString));
                    services.AddScoped<IWrapperDbConnection>(_ => new FactoryPostgreSqlConnection(connectionString!));
                } else {
                    services.AddScoped(_ => new SqlConnection(connectionString));
                    services.AddScoped<IWrapperDbConnection>(_ => new FactorySQlConnection(connectionString!));
                }

                // REPOSITORY & VECTOR STORE (Implementazioni specifiche)
                //services.AddScoped<ILogRepository, SqlLogRepository>();
                //services.AddScoped<ITraceRepository<TraceRecord>, SqlTraceRepository>();
                //services.AddScoped<IMetricRepository, SqlMetricRepository>();
                services.AddScoped<ILogVectorStore, SqlLogVectorStore>();
            };
        }
    }
}