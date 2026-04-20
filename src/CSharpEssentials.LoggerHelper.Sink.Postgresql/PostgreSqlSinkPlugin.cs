using NpgsqlTypes;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Postgresql;

// ── Options ───────────────────────────────────────────────────────

public sealed class PostgreSqlSinkOptions {
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "logs";
    public string SchemaName { get; set; } = "public";
    public bool NeedAutoCreateTable { get; set; } = true;

    /// <summary>
    /// Column definitions for the PostgreSQL table.
    /// If null/empty, default columns are used (ApplicationName, message, level, etc.).
    /// </summary>
    public List<PostgreSqlColumnConfig>? Columns { get; set; }
}

/// <summary>
/// Configuration for a PostgreSQL log column.
/// Writer types: Rendered, Template, Level, Timestamp, Exception, Serialized, Properties, Single.
/// </summary>
public sealed class PostgreSqlColumnConfig {
    /// <summary>Column name in the database table.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Writer type: Rendered, Template, Level, Timestamp, Exception, Serialized, Properties, Single.
    /// </summary>
    public string Writer { get; set; } = "Single";

    /// <summary>
    /// NpgsqlDbType as string: Text, Varchar, Jsonb, Timestamp, etc.
    /// </summary>
    public string Type { get; set; } = "Text";

    /// <summary>
    /// For "Single" writer: the Serilog property name to extract. Defaults to column Name.
    /// </summary>
    public string? Property { get; set; }
}

// ── Builder extension ─────────────────────────────────────────────

public static class PostgreSqlBuilderExtensions {
    public static LoggerHelperBuilder ConfigurePostgreSql(this LoggerHelperBuilder builder, Action<PostgreSqlSinkOptions> configure)
        => builder.ConfigureSink("PostgreSql", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

internal sealed class PostgreSqlSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sinkName, "PostgreSql", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<PostgreSqlSinkOptions>("PostgreSql")
                   ?? options.BindSinkSection<PostgreSqlSinkOptions>("PostgreSql");
        if (opts is null) {
            SelfLog.WriteLine("PostgreSQL sink configured in routes but no Sinks.PostgreSql options provided.");
            return;
        }

        var columns = opts.Columns is { Count: > 0 }
            ? BuildColumnsFromConfig(opts.Columns)
            : BuildDefaultColumns();

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.PostgreSQL(
                connectionString: opts.ConnectionString,
                tableName: opts.TableName,
                schemaName: opts.SchemaName,
                needAutoCreateTable: opts.NeedAutoCreateTable,
                columnOptions: columns
            )
        );
    }

    private static Dictionary<string, ColumnWriterBase> BuildColumnsFromConfig(List<PostgreSqlColumnConfig> columnDefs) {
        var result = new Dictionary<string, ColumnWriterBase>();

        foreach (var col in columnDefs) {
            var dbType = ParseNpgsqlDbType(col.Type);
            result[col.Name] = col.Writer switch {
                "Rendered" => new RenderedMessageColumnWriter(dbType),
                "Template" => new MessageTemplateColumnWriter(dbType),
                "Level" => new LevelColumnWriter(true, dbType),
                "Timestamp" or "timestamp" => new TimestampColumnWriter(dbType),
                "Exception" => new ExceptionColumnWriter(dbType),
                "Serialized" => new LogEventSerializedColumnWriter(dbType),
                "Properties" => new PropertiesColumnWriter(dbType),
                "Single" => new SinglePropertyColumnWriter(
                    col.Property ?? col.Name,
                    PropertyWriteMethod.ToString,
                    dbType),
                _ => throw new InvalidOperationException($"Writer '{col.Writer}' not supported. Use: Rendered, Template, Level, Timestamp, Exception, Serialized, Properties, Single.")
            };
        }

        return result;
    }

    private static Dictionary<string, ColumnWriterBase> BuildDefaultColumns() => new() {
        { "ApplicationName", new SinglePropertyColumnWriter("ApplicationName", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
        { "message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
        { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
        { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
        { "raise_date", new TimestampColumnWriter(NpgsqlDbType.Timestamp) },
        { "exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
        { "properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
        { "MachineName", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
        { "Action", new SinglePropertyColumnWriter("Action", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
        { "IdTransaction", new SinglePropertyColumnWriter("IdTransaction", PropertyWriteMethod.ToString, NpgsqlDbType.Text) }
    };

    private static NpgsqlDbType ParseNpgsqlDbType(string type) => type.ToLowerInvariant() switch {
        "text" => NpgsqlDbType.Text,
        "jsonb" => NpgsqlDbType.Jsonb,
        "varchar" => NpgsqlDbType.Varchar,
        "timestamp" => NpgsqlDbType.Timestamp,
        _ => NpgsqlDbType.Text
    };
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new PostgreSqlSinkPlugin());
}
