using Serilog;
using Serilog.Debugging;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Seq;

// ── Options ───────────────────────────────────────────────────────

public sealed class SeqSinkOptions {
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>Legacy JSON key: serverUrl</summary>
    public string? serverUrl { set => ServerUrl = value ?? ServerUrl; }

    public string? ApiKey { get; set; }
}

// ── Builder extension ─────────────────────────────────────────────

public static class SeqBuilderExtensions {
    public static LoggerHelperBuilder ConfigureSeq(this LoggerHelperBuilder builder, Action<SeqSinkOptions> configure)
        => builder.ConfigureSink("Seq", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

[LoggerHelperSink]
public sealed class SeqSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "Seq", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<SeqSinkOptions>("Seq")
                   ?? options.BindSinkSection<SeqSinkOptions>("Seq")
                   ?? options.BindSinkSection<SeqSinkOptions>("SeqOptions");
        if (opts is null) {
            SelfLog.WriteLine("Seq sink configured in routes but no Sinks.Seq options provided.");
            return;
        }

        loggerConfig.WriteTo.Conditional(
            evt => routing.ShouldEmit(evt.Level),
            wt => wt.Seq(
                serverUrl: opts.ServerUrl,
                apiKey: opts.ApiKey
            )
        );
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new SeqSinkPlugin());
}
