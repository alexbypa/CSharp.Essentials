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
    /// Allows adding custom Serilog enrichers to the pipeline.
    /// </summary>
    public LoggerHelperBuilder WithEnrichers(Action<LoggerConfiguration> configure) {
        CustomEnrichers = configure;
        return this;
    }
}
