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

    /// <summary>
    /// Configuration for the built-in sensitive data masking enricher.
    /// Maps to JSON section "LoggerHelper:SensitiveDataMasking".
    /// </summary>
    public SensitiveDataMaskingOptions SensitiveDataMasking { get; set; } = new();

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
    /// Copies sink configurations set via the fluent API into this instance.
    /// Fluent values override any JSON-bound entry for the same sink.
    /// </summary>
    internal void MergeSinkConfigsFrom(LoggerHelperOptions other) {
        foreach (var (sinkName, config) in other._sinkConfigs)
            _sinkConfigs[sinkName] = config;
    }

    /// <summary>
    /// Merges fluent-API options into this (JSON-bound) instance.
    /// Routes are additive; ApplicationName and sink configs from fluent override JSON;
    /// General: SelfLogging/RequestResponse/RenderedMessage are OR-ed, OpenTelemetry is AND-ed
    /// (so fluent can only turn it off); masking is merged only when enabled via fluent,
    /// and masking rules are de-duplicated by Pattern (the first one wins, so JSON over fluent).
    /// </summary>
    internal void MergeFluentFrom(LoggerHelperOptions fluent) {
        Routes.AddRange(fluent.Routes);
        MergeSinkConfigsFrom(fluent);
        if (!string.IsNullOrEmpty(fluent.ApplicationName))
            ApplicationName = fluent.ApplicationName;

        General.EnableSelfLogging |= fluent.General.EnableSelfLogging;
        General.EnableRequestResponseLogging |= fluent.General.EnableRequestResponseLogging;
        General.EnableRenderedMessage |= fluent.General.EnableRenderedMessage;
        General.EnableOpenTelemetry &= fluent.General.EnableOpenTelemetry;

        var m = fluent.SensitiveDataMasking;
        if (!m.Enabled)
            return;
        var target = SensitiveDataMasking;
        target.Enabled = true;
        target.Presets = target.Presets.Union(m.Presets, StringComparer.OrdinalIgnoreCase).ToList();
        target.SensitiveProperties = target.SensitiveProperties.Union(m.SensitiveProperties, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var rule in m.Rules)
            if (!target.Rules.Exists(r => string.Equals(r.Pattern, rule.Pattern, StringComparison.Ordinal)))
                target.Rules.Add(rule);
        if (m.MaskText != new SensitiveDataMaskingOptions().MaskText)
            target.MaskText = m.MaskText;
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

    /// <summary>
    /// Enable RenderedMessage enricher (adds pre-rendered message string to each log event).
    /// Useful for database sinks that need a column with the formatted message.
    /// Disabled by default to reduce per-log allocations.
    /// </summary>
    public bool EnableRenderedMessage { get; set; }

    /// <summary>
    /// Enable contextual error logging: retains recent Debug/Info/Warning logs in a ring buffer
    /// and flushes them when an Error/Fatal occurs, providing crash context.
    /// </summary>
    public bool EnableContextualLogging { get; set; }

    /// <summary>
    /// Number of log entries to retain in the contextual ring buffer. Default: 100. Must be greater than 0 (a value &lt;= 0 makes <c>ContextualLogBuffer</c> throw <see cref="ArgumentOutOfRangeException"/> at startup).
    /// </summary>
    public int ContextualBufferCapacity { get; set; } = 100;
}
