using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Diagnostics.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public static class CustomMetrics {
    private static string? _connString;
    public static void Initialize(IConfiguration configuration) {
        _connString = configuration.GetConnectionString("MetricsDb");
    }
    public static readonly Meter Meter = new("LoggerHelper.Metrics", "1.0");
    public static int CurrentSecond => DateTime.UtcNow.Second;
    //TODO: Aggiungere un pattern per aggiungere le metriche desiderate esternamente !
    public static readonly GaugeWrapper<int> CurrentSecondGauge =
                new(Meter, "current_second", () => DateTime.UtcNow.Second, "seconds", "Current second of the minute");

    public static readonly GaugeWrapper<double> MemoryUsedGauge =
        new(Meter, "memory_used_mb", () => {
            var bytes = GC.GetTotalMemory(false);
            return Math.Round(bytes / 1024.0 / 1024.0, 2);
        }, "MB", "Managed memory used (approx)");

    public static readonly GaugeWrapper<long> ActivePostgresConnectionsGauge =
    new(Meter, "postgresql.connections.active", GetActiveConnectionCount, "conn", "Active PostgreSQL connections");
    private static long GetActiveConnectionCount() {
        try {
            using var conn = new NpgsqlConnection(_connString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE state = 'active'", conn);
            var result = cmd.ExecuteScalar();

            return result is long l ? l : Convert.ToInt64(result);
        } catch {
            return -1; // valore segnaposto in caso di errore
        }
    }
}
