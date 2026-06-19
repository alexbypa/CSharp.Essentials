using Serilog;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Fluent builder for configuring LoggerHelper.
/// Use this as an alternative (or complement) to JSON configuration.
///
/// Example:
///   services.AddLoggerHelper(b => b
///       .WithApplicationName("MyApp")
///       .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
///       .AddRoute("Email", LogEventLevel.Error, LogEventLevel.Fatal)
///       .ConfigureSink&lt;EmailSinkOptions&gt;("Email", e => { e.To = "ops@example.com"; })
///   );
///
/// Per-sink convenience methods (ConfigureEmail, ConfigureFile, etc.)
/// are provided as extension methods by each sink package.
/// </summary>
public sealed class LoggerHelperBuilder {
    internal LoggerHelperOptions Options { get; } = new();
    internal Action<LoggerConfiguration>? CustomEnrichers { get; private set; }

    /// <summary>
    /// Sets the application name enriched on every log entry.
    /// </summary>
    public LoggerHelperBuilder WithApplicationName(string name) {
        Options.ApplicationName = name;
        return this;
    }

    /// <summary>
    /// Adds a routing rule: forward the specified log levels to the named sink.
    /// </summary>
    public LoggerHelperBuilder AddRoute(string sinkName, params LogEventLevel[] levels) {
        Options.Routes.Add(new SinkRouting {
            Sink = sinkName,
            Levels = levels.Select(l => l.ToString()).ToList()
        });
        return this;
    }

    /// <summary>
    /// Adds a routing rule with probabilistic sampling: forward only a fraction
    /// of matching log events to the named sink. Use this to reduce volume on
    /// expensive sinks (Elasticsearch, SQL) in high-throughput apps.
    /// </summary>
    /// <param name="sinkName">Target sink (e.g., "Elasticsearch").</param>
    /// <param name="samplingRate">Fraction to forward (0.0–1.0). 1.0 = 100%, 0.1 = 10%.</param>
    /// <param name="levels">Log levels to route.</param>
    public LoggerHelperBuilder AddRoute(string sinkName, double samplingRate, params LogEventLevel[] levels) {
        Options.Routes.Add(new SinkRouting {
            Sink = sinkName,
            Levels = levels.Select(l => l.ToString()).ToList(),
            SamplingRate = samplingRate
        });
        return this;
    }

    /// <summary>
    /// Adds a routing rule for all levels to the named sink.
    /// </summary>
    public LoggerHelperBuilder AddRouteAll(string sinkName) {
        Options.Routes.Add(new SinkRouting {
            Sink = sinkName,
            Levels = Enum.GetValues<LogEventLevel>().Select(l => l.ToString()).ToList()
        });
        return this;
    }

    /// <summary>
    /// Configures a sink's options using the generic, extensible mechanism.
    /// Each sink package also provides a strongly-typed extension method
    /// (e.g., ConfigureEmail, ConfigureFile) built on top of this.
    /// </summary>
    public LoggerHelperBuilder ConfigureSink<T>(string sinkName, Action<T> configure) where T : class, new() {
        var opts = Options.GetOrAddSinkConfig<T>(sinkName);
        configure(opts);
        return this;
    }

    // --- General options ---

    public LoggerHelperBuilder EnableSelfLogging() {
        Options.General.EnableSelfLogging = true;
        return this;
    }

    public LoggerHelperBuilder EnableRequestResponseLogging() {
        Options.General.EnableRequestResponseLogging = true;
        return this;
    }

    public LoggerHelperBuilder DisableOpenTelemetry() {
        Options.General.EnableOpenTelemetry = false;
        return this;
    }

    /// <summary>
    /// Enables RenderedMessage enricher — adds a pre-rendered message string to each log event.
    /// Useful for database sinks (MSSqlServer, PostgreSQL) that need a formatted message column.
    /// Disabled by default to minimize per-log allocations.
    /// </summary>
    public LoggerHelperBuilder EnableRenderedMessage() {
        Options.General.EnableRenderedMessage = true;
        return this;
    }

    /// <summary>
    /// Allows adding custom Serilog enrichers to the pipeline.
    /// </summary>
    public LoggerHelperBuilder WithEnrichers(Action<LoggerConfiguration> configure) {
        CustomEnrichers = configure;
        return this;
    }

    /// <summary>
    /// Enables the built-in sensitive data masking enricher — redacts PII/secrets
    /// (emails, credit cards, JWT/Bearer tokens, connection-string passwords, and any
    /// custom regex or named property) from every log event before it reaches a sink.
    /// </summary>
    /// <param name="configure">Optional callback to select presets, custom rules, and sensitive property names.</param>
    public LoggerHelperBuilder EnableSensitiveDataMasking(Action<SensitiveDataMaskingOptions>? configure = null) {
        Options.SensitiveDataMasking.Enabled = true;
        configure?.Invoke(Options.SensitiveDataMasking);
        return this;
    }
}
