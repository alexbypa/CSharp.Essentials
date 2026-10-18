using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Elasticsearch;

// ── Options ───────────────────────────────────────────────────────

public sealed class ElasticsearchSinkOptions {
    public string NodeUris { get; set; } = string.Empty;

    /// <summary>Legacy JSON key: nodeUris</summary>
    public string? nodeUris { set => NodeUris = value ?? NodeUris; }

    public string? IndexFormat { get; set; }

    /// <summary>Legacy JSON key: indexFormat</summary>
    public string? indexFormat { set => IndexFormat = value ?? IndexFormat; }

    /// <summary>
    /// Registers the index template on startup (default: true).
    /// Set to false when the template is managed externally or the cluster user lacks permission to create templates.
    /// </summary>
    public bool AutoRegisterTemplate { get; set; } = true;

    /// <summary>Legacy JSON key: autoRegisterTemplate</summary>
    public bool? autoRegisterTemplate { set => AutoRegisterTemplate = value ?? AutoRegisterTemplate; }

    /// <summary>
    /// Minimum interval between two events passed to this sink route. Null (default) or zero disables throttling.
    /// JSON example: "00:00:20"
    /// </summary>
    public TimeSpan? ThrottleInterval { get; set; }
}

// ── Builder extension ─────────────────────────────────────────────

public static class ElasticsearchBuilderExtensions {
    public static LoggerHelperBuilder ConfigureElasticsearch(this LoggerHelperBuilder builder, Action<ElasticsearchSinkOptions> configure)
        => builder.ConfigureSink("Elasticsearch", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

[LoggerHelperSink]
public sealed class ElasticsearchSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "Elasticsearch", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sinkName, "ElasticSearch", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<ElasticsearchSinkOptions>("Elasticsearch")
                   ?? options.BindSinkSection<ElasticsearchSinkOptions>("Elasticsearch")
                   ?? options.BindSinkSection<ElasticsearchSinkOptions>("ElasticSearch");
        if (opts is null)
            throw new InvalidOperationException("Elasticsearch sink configured in routes but no Sinks.Elasticsearch options provided (LoggerHelper:Sinks:Elasticsearch).");
        if (string.IsNullOrWhiteSpace(opts.NodeUris))
            throw new InvalidOperationException("Elasticsearch sink: NodeUris is required (LoggerHelper:Sinks:Elasticsearch:NodeUris).");

        var gate = new ThrottleGate("Elasticsearch", opts.ThrottleInterval);

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level) && gate.TryPass(evt),
            wt => wt.Elasticsearch(
                nodeUris: opts.NodeUris,
                indexFormat: opts.IndexFormat,
                autoRegisterTemplate: opts.AutoRegisterTemplate,
                detectElasticsearchVersion: false,
                autoRegisterTemplateVersion : AutoRegisterTemplateVersion.ESv7
            )
        );
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new ElasticsearchSinkPlugin());
}
