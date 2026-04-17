using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Root configuration model for LoggerHelper.
/// Maps to JSON section "LoggerHelper" in appsettings.
/// Also populated by the fluent builder API.
/// </summary>
public sealed class LoggerHelperOptions {
    /// <summary>
    /// Application name attached to every log entry.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Routing rules: which log levels go to which sinks.
    /// </summary>
    public List<SinkRouting> Routes { get; set; } = [];

    /// <summary>
    /// General configuration flags.
    /// </summary>
    public GeneralOptions General { get; set; } = new();

    // ── Extensible sink configuration (OCP) ──────────────────────
    // Each sink package defines its own options class and registers it here
    // via the fluent API or JSON binding. The core never knows about specific sinks.

    private readonly Dictionary<string, object> _sinkConfigs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sets the configuration object for a named sink.
    /// </summary>
    public void SetSinkConfig<T>(string sinkName, T config) where T : class =>
        _sinkConfigs[sinkName] = config;

    /// <summary>
    /// Gets the configuration object for a named sink, or null if not configured.
    /// </summary>
    public T? GetSinkConfig<T>(string sinkName) where T : class =>
        _sinkConfigs.TryGetValue(sinkName, out var v) ? v as T : null;

    /// <summary>
    /// Gets or creates the configuration object for a named sink.
    /// Used by fluent builder extension methods.
    /// </summary>
    public T GetOrAddSinkConfig<T>(string sinkName) where T : class, new() {
        if (_sinkConfigs.TryGetValue(sinkName, out var v) && v is T typed)
            return typed;
        typed = new T();
        _sinkConfigs[sinkName] = typed;
        return typed;
    }

    /// <summary>
    /// Binds a sink configuration section from JSON and stores it in the dictionary.
    /// Returns null if the section doesn't exist.
    /// Called by sink plugins during Configure() for JSON fallback.
    /// </summary>
    public T? BindSinkSection<T>(string sinkName) where T : class, new() {
        var section = RawSinksSection?.GetSection(sinkName);
        if (section is null || !section.Exists())
            return null;
        var opts = new T();
        section.Bind(opts);
        _sinkConfigs[sinkName] = opts;
        return opts;
    }

    /// <summary>
    /// Raw IConfigurationSection for "Sinks", stored by the JSON config loader.
    /// Sink plugins use this for JSON binding fallback.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IConfigurationSection? RawSinksSection { get; internal set; }
}

/// <summary>
/// General configuration flags.
/// </summary>
public sealed class GeneralOptions {
    /// <summary>
    /// Enable Serilog SelfLog for internal diagnostics.
    /// </summary>
    public bool EnableSelfLogging { get; set; }

    /// <summary>
    /// Enable request/response HTTP logging middleware.
    /// </summary>
    public bool EnableRequestResponseLogging { get; set; }

    /// <summary>
    /// Enable OpenTelemetry trace correlation on log events.
    /// </summary>
    public bool EnableOpenTelemetry { get; set; } = true;
}
