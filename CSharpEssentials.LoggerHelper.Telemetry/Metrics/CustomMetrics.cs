using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Diagnostics.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Metrics;
/// <summary>
/// Defines and registers custom metrics (observable gauges) for the application.
/// This static class exposes a Meter and two ObservableGauge instruments:
/// 1. memory_used_mb – reports the managed memory usage in megabytes.
/// 2. postgresql.connections.active – reports the number of active PostgreSQL connections.
/// 
/// To function correctly, call <see cref="Initialize(IConfiguration)"/> at startup
/// so that the connection string is loaded and the gauges can query the database.
/// </summary>
public static class CustomMetrics {
    /// <summary>
    /// Holds the PostgreSQL connection string for querying active connections.
    /// This value is set during initialization.
    /// </summary>
    private static string? _connString;
    /// <summary>
    /// Reads the “MetricsDb” connection string from configuration.
    /// Must be invoked once at application startup before any gauge callbacks run.
    /// </summary>
    /// <param name="configuration">
    /// The application’s configuration (e.g., from <c>appsettings.json</c>).
    /// </param>
    public static void Initialize(IConfiguration configuration) {
        _connString = configuration.GetConnectionString("MetricsDb");
    }
    //TODO: Passare nome metrica e versione
    /// <summary>
    /// The Meter that groups all custom metrics under a single namespace and version.
    /// All ObservableGauge instruments in this class are created from this Meter.
    /// </summary>
    public static readonly Meter Meter = new("LoggerHelper.Metrics", "1.0");
    public static int CurrentSecond => DateTime.UtcNow.Second;
    /// <summary>
    /// ObservableGauge that reports the total managed memory used by the .NET GC, in megabytes.
    /// 
    /// Each time the metric is collected, it invokes the provided callback:
    ///   - Calls GC.GetTotalMemory(false) to get the total bytes.
    ///   - Converts bytes to megabytes (rounded to two decimal places).
    /// 
    /// Unit: “MB”
    /// Description: “Managed memory used (approx)”
    /// </summary>
    public static readonly GaugeWrapper<double> MemoryUsedGauge =
        new(Meter, "memory_used_mb", () => {
            var bytes = GC.GetTotalMemory(false);
            return Math.Round(bytes / 1024.0 / 1024.0, 2);
        }, "MB", "Managed memory used (approx)");
    /// <summary>
    /// ObservableGauge that reports the number of active connections to the configured PostgreSQL instance.
    /// 
    /// Each time the metric is collected, it invokes <see cref="GetActiveConnectionCount"/>.
    /// Unit: “conn”
    /// Description: “Active PostgreSQL connections”
    /// </summary>
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
            return -1;
        }
    }
}
