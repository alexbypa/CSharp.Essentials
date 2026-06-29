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
        LoadedSinkStore loadedSinkStore,
        ISinkPluginRegistry registry,
        IPluginDiscovery pluginDiscovery,
        Action<LoggerConfiguration>? customEnrichers,
        out ContextualLogBuffer? contextBuffer,
        IContextLogEnricher? contextEnricher = null) {

        contextBuffer = null;

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
            .Enrich.FromLogContext();

        // RenderedMessage enricher is opt-in: it allocates a string per log event.
        // Enable only when database sinks need a pre-rendered message column.
        if (options.General.EnableRenderedMessage)
            loggerConfig.Enrich.With<RenderedMessageEnricher>();

        // Sensitive data masking is opt-in: redacts PII/secrets from structured properties
        // (and RenderedMessage, when enabled) before any sink receives the event.
        if (options.SensitiveDataMasking.Enabled)
            loggerConfig.Enrich.With(new SensitiveDataMaskingEnricher(options.SensitiveDataMasking));

        if (options.General.EnableOpenTelemetry)
            loggerConfig.WriteTo.Sink(new OpenTelemetryLogEventSink());

        // Apply custom enrichers
        customEnrichers?.Invoke(loggerConfig);

        if (contextEnricher is not null)
            loggerConfig = contextEnricher.Enrich(loggerConfig);

        // Discover plugins and configure routing
        var engine = new SinkRoutingEngine(options, errorStore, loadedSinkStore, registry, pluginDiscovery);
        engine.ConfigureRoutes(loggerConfig);

        // Contextual error logging: ring buffer for crash context
        LoggerHolder? contextLoggerHolder = null;
        if (options.General.EnableContextualLogging) {
            contextBuffer = new ContextualLogBuffer(options.General.ContextualBufferCapacity);
            contextLoggerHolder = new LoggerHolder();
            var contextSink = new ContextualLogSink(contextBuffer, contextLoggerHolder);
            loggerConfig.WriteTo.Sink(contextSink, Serilog.Events.LogEventLevel.Debug);
        }

        var logger = loggerConfig.CreateLogger();

        // Set the flush target now that the logger is built
        if (contextLoggerHolder is not null)
            contextLoggerHolder.Logger = logger;

        return logger;
    }
}
