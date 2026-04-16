using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Elasticsearch;

// ── Options ───────────────────────────────────────────────────────

public sealed class ElasticsearchSinkOptions {
    public string NodeUris { get; set; } = string.Empty;
    public string? IndexFormat { get; set; }
}

// ── Builder extension ─────────────────────────────────────────────

public static class ElasticsearchBuilderExtensions {
    public static LoggerHelperBuilder ConfigureElasticsearch(this LoggerHelperBuilder builder, Action<ElasticsearchSinkOptions> configure)
        => builder.ConfigureSink("Elasticsearch", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

internal sealed class ElasticsearchSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "Elasticsearch", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sinkName, "ElasticSearch", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<ElasticsearchSinkOptions>("Elasticsearch")
                   ?? options.BindSinkSection<ElasticsearchSinkOptions>("Elasticsearch");
        if (opts is null) {
            SelfLog.WriteLine("Elasticsearch sink configured in routes but no Sinks.Elasticsearch options provided.");
            return;
        }

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.Elasticsearch(
                nodeUris: opts.NodeUris,
                indexFormat: opts.IndexFormat,
                autoRegisterTemplate: true,
                restrictedToMinimumLevel: LogEventLevel.Verbose
            )
        );
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new ElasticsearchSinkPlugin());
}
