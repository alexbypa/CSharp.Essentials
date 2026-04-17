using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog;
using Serilog.Debugging;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Responsible for building the Serilog pipeline from LoggerHelperOptions.
/// Extracted from ServiceCollectionExtensions to respect Single Responsibility.
/// </summary>
internal static class LoggerPipelineFactory {
    /// <summary>
    /// Builds the complete Serilog logger with enrichers, OpenTelemetry, custom enrichers, and sink routing.
    /// </summary>
    internal static Serilog.ILogger Build(
        LoggerHelperOptions options,
        ILogErrorStore errorStore,
        ISinkPluginRegistry registry,
        IPluginDiscovery pluginDiscovery,
        Action<LoggerConfiguration>? customEnrichers) {

        // Enable SelfLog if requested
        if (options.General.EnableSelfLogging)
            SelfLog.Enable(msg => errorStore.Add(new LogErrorEntry {
                SinkName = "SelfLog",
                ErrorMessage = msg
            }));

        // Build the Serilog pipeline
        var loggerConfig = new LoggerConfiguration()
            .Enrich.WithProperty("ApplicationName", options.ApplicationName)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.FromLogContext()
            .Enrich.With<RenderedMessageEnricher>();

        if (options.General.EnableOpenTelemetry)
            loggerConfig.WriteTo.Sink(new OpenTelemetryLogEventSink());

        // Apply custom enrichers
        customEnrichers?.Invoke(loggerConfig);

        // Discover plugins and configure routing
        var engine = new SinkRoutingEngine(options, errorStore, registry, pluginDiscovery);
        engine.ConfigureRoutes(loggerConfig);

        return loggerConfig.CreateLogger();
    }
}
