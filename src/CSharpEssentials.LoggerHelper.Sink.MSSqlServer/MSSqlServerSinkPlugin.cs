using Serilog;
using Serilog.Debugging;
using System.Data;
using System.Runtime.CompilerServices;
using SerilogMSSqlOptions = Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions;

namespace CSharpEssentials.LoggerHelper.Sink.MSSqlServer;

// ── Options ───────────────────────────────────────────────────────

public sealed class MSSqlServerSinkOptions {
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "Logs";
    public string SchemaName { get; set; } = "dbo";
    public bool AutoCreateSqlTable { get; set; } = true;
    public int BatchPostingLimit { get; set; } = 100;
    public string Period { get; set; } = "0.00:00:10";

    /// <summary>
    /// Standard columns to include (e.g. "LogEvent", "Message", "MessageTemplate", "Level", "Exception").
    /// </summary>
    public List<string>? AddStandardColumns { get; set; }

    /// <summary>
    /// Standard columns to remove (e.g. "Properties").
    /// </summary>
    public List<string>? RemoveStandardColumns { get; set; }

    /// <summary>
    /// Additional custom columns mapped from log properties.
    /// Each column maps to a Serilog property with the same name.
    /// </summary>
    public List<AdditionalColumnConfig>? AdditionalColumns { get; set; }
}

/// <summary>
/// Configuration for an additional SQL column.
/// </summary>
public sealed class AdditionalColumnConfig {
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = "NVarChar";
    public bool AllowNull { get; set; } = true;
    public int DataLength { get; set; } = -1;
}

// ── Builder extension ─────────────────────────────────────────────

public static class MSSqlServerBuilderExtensions {
    public static LoggerHelperBuilder ConfigureMSSqlServer(this LoggerHelperBuilder builder, Action<MSSqlServerSinkOptions> configure)
        => builder.ConfigureSink("MSSqlServer", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

internal sealed class MSSqlServerSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "MSSqlServer", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<MSSqlServerSinkOptions>("MSSqlServer")
                   ?? options.BindSinkSection<MSSqlServerSinkOptions>("MSSqlServer");
        if (opts is null) {
            SelfLog.WriteLine("MSSqlServer sink configured in routes but no Sinks.MSSqlServer options provided.");
            return;
        }

        var colOptions = BuildColumnOptions(opts);

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.MSSqlServer(
                connectionString: opts.ConnectionString,
                sinkOptions: new SerilogMSSqlOptions {
                    TableName = opts.TableName,
                    SchemaName = opts.SchemaName,
                    AutoCreateSqlTable = opts.AutoCreateSqlTable,
                    BatchPostingLimit = opts.BatchPostingLimit,
                    BatchPeriod = TimeSpan.TryParse(opts.Period, out var p) ? p : TimeSpan.FromSeconds(10)
                },
                columnOptions: colOptions
            )
        );
    }

    private static Serilog.Sinks.MSSqlServer.ColumnOptions BuildColumnOptions(MSSqlServerSinkOptions opts) {
        var colOptions = new Serilog.Sinks.MSSqlServer.ColumnOptions();

        // Standard columns: add
        if (opts.AddStandardColumns is { Count: > 0 }) {
            colOptions.Store.Clear();
            foreach (var col in opts.AddStandardColumns) {
                if (Enum.TryParse<Serilog.Sinks.MSSqlServer.StandardColumn>(col, true, out var parsed))
                    colOptions.Store.Add(parsed);
            }
        }

        // Standard columns: remove
        if (opts.RemoveStandardColumns is { Count: > 0 }) {
            foreach (var col in opts.RemoveStandardColumns) {
                if (Enum.TryParse<Serilog.Sinks.MSSqlServer.StandardColumn>(col, true, out var parsed))
                    colOptions.Store.Remove(parsed);
            }
        }

        // Additional columns (custom properties → SQL columns)
        if (opts.AdditionalColumns is { Count: > 0 }) {
            colOptions.AdditionalColumns = [];
            foreach (var col in opts.AdditionalColumns) {
                colOptions.AdditionalColumns.Add(new Serilog.Sinks.MSSqlServer.SqlColumn {
                    ColumnName = col.ColumnName,
                    DataType = Enum.TryParse<SqlDbType>(col.DataType, true, out var dt) ? dt : SqlDbType.NVarChar,
                    AllowNull = col.AllowNull,
                    DataLength = col.DataLength
                });
            }
        }

        return colOptions;
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new MSSqlServerSinkPlugin());
}
