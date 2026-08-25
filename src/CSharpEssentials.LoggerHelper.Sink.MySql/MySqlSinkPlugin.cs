using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.PeriodicBatching;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.MySql;

// ── Options ───────────────────────────────────────────────────────

public sealed class MySqlSinkOptions {
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Legacy JSON key: connectionstring</summary>
    public string? connectionstring { set => ConnectionString = value ?? ConnectionString; }

    public string TableName { get; set; } = "logs";

    /// <summary>Legacy JSON key: tableName</summary>
    public string? tableName { set => TableName = value ?? TableName; }

    /// <summary>Creates the table (and its columns) at startup when it does not exist.</summary>
    public bool AutoCreateTable { get; set; } = true;

    /// <summary>Legacy JSON key: autoCreateTable</summary>
    public bool? autoCreateTable { set { if (value.HasValue) AutoCreateTable = value.Value; } }

    /// <summary>Legacy JSON key alias shared with the PostgreSQL sink: needAutoCreateTable</summary>
    public bool? needAutoCreateTable { set { if (value.HasValue) AutoCreateTable = value.Value; } }

    /// <summary>When true, adds an Id BIGINT AUTO_INCREMENT PRIMARY KEY column to the created table.</summary>
    public bool AddAutoIncrementColumn { get; set; } = true;

    /// <summary>Legacy JSON key: addAutoIncrementColumn</summary>
    public bool addAutoIncrementColumn { set => AddAutoIncrementColumn = value; }

    /// <summary>Store timestamps as UTC instead of local time.</summary>
    public bool StoreTimestampInUtc { get; set; }

    /// <summary>Max number of events written per batch. Range 1..1000.</summary>
    public int BatchPostingLimit { get; set; } = 100;

    /// <summary>Interval between batch flushes, as a TimeSpan string.</summary>
    public string Period { get; set; } = "0.00:00:05";

    /// <summary>
    /// Column definitions for the MySQL table.
    /// If null/empty, default columns are used (ApplicationName, message, level, etc.).
    /// </summary>
    public List<MySqlColumnConfig>? Columns { get; set; }

    /// <summary>Legacy JSON key: ColumnsMySql</summary>
    public List<MySqlColumnConfig>? ColumnsMySql { set => Columns = value ?? Columns; }
}

/// <summary>
/// Configuration for a MySQL log column.
/// Writer types: Rendered, Template, Level, Timestamp, Exception, Serialized, Properties, Single.
/// </summary>
public sealed class MySqlColumnConfig {
    /// <summary>Column name in the database table.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Writer type: Rendered, Template, Level, Timestamp, Exception, Serialized, Properties, Single.
    /// </summary>
    public string Writer { get; set; } = "Single";

    /// <summary>
    /// MySQL column type as string: Text, LongText, Json, VarChar, DateTime, Int, BigInt, Bool.
    /// </summary>
    public string Type { get; set; } = "Text";

    /// <summary>
    /// For "Single" writer: the Serilog property name to extract. Defaults to column Name.
    /// </summary>
    public string? Property { get; set; }

    /// <summary>Length applied to VarChar columns during auto-creation.</summary>
    public int Length { get; set; } = 255;
}

// ── Builder extension ─────────────────────────────────────────────

public static class MySqlBuilderExtensions {
    public static LoggerHelperBuilder ConfigureMySql(this LoggerHelperBuilder builder, Action<MySqlSinkOptions> configure)
        => builder.ConfigureSink("MySql", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

[LoggerHelperSink]
public sealed class MySqlSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "MySql", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sinkName, "MySQL", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sinkName, "MariaDB", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<MySqlSinkOptions>("MySql")
                   ?? options.BindSinkSection<MySqlSinkOptions>("MySql")
                   ?? options.BindSinkSection<MySqlSinkOptions>("MySQL")
                   ?? options.BindSinkSection<MySqlSinkOptions>("MariaDB");
        if (opts is null) {
            SelfLog.WriteLine("MySQL sink configured in routes but no Sinks.MySql options provided.");
            return;
        }

        if (string.IsNullOrWhiteSpace(opts.ConnectionString)) {
            SelfLog.WriteLine("MySQL sink skipped: ConnectionString is empty.");
            return;
        }

        var columns = opts.Columns is { Count: > 0 }
            ? MySqlColumnMap.FromConfig(opts.Columns)
            : MySqlColumnMap.Default();

        var batchLimit = Math.Clamp(opts.BatchPostingLimit, 1, 1000);
        if (batchLimit != opts.BatchPostingLimit)
            SelfLog.WriteLine($"MySQL: BatchPostingLimit {opts.BatchPostingLimit} out of range 1..1000, clamped to {batchLimit}.");

        if (!TimeSpan.TryParse(opts.Period, out var period) || period <= TimeSpan.Zero)
            period = TimeSpan.FromSeconds(5);

        var batched = new MySqlBatchedSink(
            connectionString: opts.ConnectionString,
            tableName: opts.TableName,
            columns: columns,
            autoCreateTable: opts.AutoCreateTable,
            addAutoIncrementColumn: opts.AddAutoIncrementColumn,
            storeTimestampInUtc: opts.StoreTimestampInUtc);

        var sink = new PeriodicBatchingSink(batched, new PeriodicBatchingSinkOptions {
            BatchSizeLimit = batchLimit,
            Period = period,
            EagerlyEmitFirstEvent = true,
            QueueLimit = 10_000
        });

        loggerConfig.WriteTo.Conditional(evt => routing.Matches(evt.Level), wt => wt.Sink(sink));
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new MySqlSinkPlugin());
}
