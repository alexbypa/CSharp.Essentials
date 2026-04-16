using Serilog;
using Serilog.Debugging;
using System.Runtime.CompilerServices;
using SerilogMSSqlOptions = Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions;

namespace CSharpEssentials.LoggerHelper.Sink.MSSqlServer;

// ── Options ───────────────────────────────────────────────────────

public sealed class MSSqlServerSinkOptions {
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "Logs";
    public string SchemaName { get; set; } = "dbo";
    public bool AutoCreateSqlTable { get; set; } = true;
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

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.MSSqlServer(
                connectionString: opts.ConnectionString,
                sinkOptions: new SerilogMSSqlOptions {
                    TableName = opts.TableName,
                    SchemaName = opts.SchemaName,
                    AutoCreateSqlTable = opts.AutoCreateSqlTable
                }
            )
        );
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new MSSqlServerSinkPlugin());
}
