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

        var columns = BuildDefaultColumns();

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
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new PostgreSqlSinkPlugin());
}
