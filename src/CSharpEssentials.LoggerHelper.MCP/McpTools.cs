using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog.Events;
using System.Text;

namespace CSharpEssentials.LoggerHelper.MCP;

/// <summary>
/// Implements MCP tools that expose LoggerHelper diagnostics to AI assistants.
/// Register via <see cref="McpExtensions.AddLoggerHelperMcp"/> and call from POST /mcp.
/// </summary>
public sealed class LoggerHelperMcpTools {
    private readonly ILogErrorStore _errorStore;
    private readonly ILoadedSinkStore _sinkStore;
    private readonly LoggerHelperOptions _options;
    private readonly ContextualLogBuffer? _contextBuffer;

    public LoggerHelperMcpTools(
        ILogErrorStore errorStore,
        ILoadedSinkStore sinkStore,
        LoggerHelperOptions options,
        ContextualLogBuffer? contextBuffer = null) {
        _errorStore    = errorStore;
        _sinkStore     = sinkStore;
        _options       = options;
        _contextBuffer = contextBuffer;
    }

    /// <summary>
    /// Returns the N most recent sink errors from <see cref="ILogErrorStore"/>.
    /// Tool name: <c>loggerhelper_get_errors</c>
    /// </summary>
    public string GetErrors(int count = 20) {
        var all = _errorStore.GetAll();
        if (all.Count == 0)
            return "No sink errors recorded.";

        var take   = Math.Min(count, all.Count);
        var recent = all.Skip(all.Count - take);
        var sb     = new StringBuilder();
        sb.AppendLine($"Recent LoggerHelper sink errors ({take} of {all.Count}):");
        sb.AppendLine();
        foreach (var e in recent) {
            sb.AppendLine($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] {e.SinkName}: {e.ErrorMessage}");
            if (!string.IsNullOrEmpty(e.ContextInfo))
                sb.AppendLine($"  Context: {e.ContextInfo}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns all loaded sink routes with their active/failed status and log levels.
    /// Tool name: <c>loggerhelper_get_sinks</c>
    /// </summary>
    public string GetSinks() {
        var sinks = _sinkStore.GetAll();
        if (sinks.Count == 0)
            return "No sinks are currently loaded.";

        var sb = new StringBuilder();
        sb.AppendLine($"Loaded LoggerHelper sinks ({sinks.Count}):");
        foreach (var s in sinks) {
            var status = s.Configured ? "ACTIVE" : "FAILED";
            sb.AppendLine($"  [{status}] {s.SinkName} | Levels: [{string.Join(", ", s.Levels)}]");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns the current LoggerHelper configuration: app name, routes, and masking state.
    /// Tool name: <c>loggerhelper_get_config</c>
    /// </summary>
    public string GetConfig() {
        var sb = new StringBuilder();
        sb.AppendLine("LoggerHelper Configuration");
        sb.AppendLine($"  Application : {_options.ApplicationName}");
        sb.AppendLine($"  Masking     : {(_options.SensitiveDataMasking.Enabled ? "enabled" : "disabled")}");
        sb.AppendLine($"  Contextual  : {(_options.General.EnableContextualLogging ? $"enabled (buffer: {_options.General.ContextualBufferCapacity})" : "disabled")}");
        sb.AppendLine($"  Routes ({_options.Routes.Count}):");
        foreach (var r in _options.Routes)
            sb.AppendLine($"    -> {r.Sink}: [{string.Join(", ", r.Levels)}]");
        return sb.ToString();
    }

    /// <summary>
    /// Returns the overall health status: OK / WARNING / CRITICAL.
    /// Tool name: <c>loggerhelper_get_health</c>
    /// </summary>
    public string GetHealth() {
        var errorCount  = _errorStore.Count;
        var sinks       = _sinkStore.GetAll();
        var activeCount = sinks.Count(s => s.Configured);
        var status      = errorCount == 0 ? "OK" : errorCount < 10 ? "WARNING" : "CRITICAL";

        return $"""
            LoggerHelper Health
              Status  : {status}
              Sinks   : {activeCount} active / {sinks.Count} configured
              Errors  : {errorCount} recorded
            """;
    }

    /// <summary>
    /// Changes the log level routing for a specific sink at runtime.
    /// Tool name: <c>loggerhelper_set_log_level</c>
    /// </summary>
    public string SetLogLevel(string sink, string levels) {
        var route = _options.Routes.FirstOrDefault(r =>
            string.Equals(r.Sink, sink, StringComparison.OrdinalIgnoreCase));

        if (route is null)
            return $"Sink '{sink}' not found in routes. Available sinks: {string.Join(", ", _options.Routes.Select(r => r.Sink))}";

        var newLevels = levels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validLevels = new List<string>();
        foreach (var l in newLevels) {
            if (Enum.TryParse<LogEventLevel>(l, ignoreCase: true, out _))
                validLevels.Add(l);
        }

        if (validLevels.Count == 0)
            return "No valid log levels provided. Valid values: Verbose, Debug, Information, Warning, Error, Fatal";

        var oldLevels = string.Join(", ", route.Levels);
        route.Levels = validLevels;
        route.InvalidateLevelCache();

        return $"Updated sink '{sink}' log levels: [{oldLevels}] -> [{string.Join(", ", validLevels)}]";
    }

    /// <summary>
    /// Searches recent log entries in the contextual ring buffer.
    /// Tool name: <c>loggerhelper_search_logs</c>
    /// </summary>
    public string SearchLogs(string? query = null, string? level = null, int count = 50) {
        if (_contextBuffer is null)
            return "Contextual logging is not enabled. Set General:EnableContextualLogging = true in appsettings.";

        var entries = _contextBuffer.Snapshot();
        if (entries.Count == 0)
            return "No log entries in the contextual buffer.";

        IEnumerable<LogBufferEntry> filtered = entries;

        if (!string.IsNullOrEmpty(level) && Enum.TryParse<LogEventLevel>(level, ignoreCase: true, out var filterLevel))
            filtered = filtered.Where(e => e.Level == filterLevel);

        if (!string.IsNullOrEmpty(query))
            filtered = filtered.Where(e =>
                (e.Message?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.SourceContext?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

        var results = filtered.TakeLast(count).ToList();

        if (results.Count == 0)
            return $"No matching log entries found for query='{query}', level='{level}'.";

        var sb = new StringBuilder();
        sb.AppendLine($"Log search results ({results.Count} entries, buffer has {entries.Count}/{_contextBuffer.Capacity}):");
        sb.AppendLine();
        foreach (var e in results)
            sb.AppendLine($"[{e.Timestamp:HH:mm:ss.fff}] [{e.Level,-11}] [{e.SourceContext ?? "?"}] {e.Message}");

        return sb.ToString();
    }

    /// <summary>
    /// Enables or disables a sink route at runtime without restart.
    /// Tool name: <c>loggerhelper_toggle_sink</c>
    /// </summary>
    public string ToggleSink(string sink, bool enabled) {
        var route = _options.Routes.FirstOrDefault(r =>
            string.Equals(r.Sink, sink, StringComparison.OrdinalIgnoreCase));

        if (route is null)
            return $"Sink '{sink}' not found. Available sinks: {string.Join(", ", _options.Routes.Select(r => r.Sink))}";

        if (enabled && route.Levels.Count > 0)
            return $"Sink '{sink}' is already enabled with levels: [{string.Join(", ", route.Levels)}]";

        if (!enabled && route.Levels.Count == 0)
            return $"Sink '{sink}' is already disabled.";

        if (!enabled) {
            route.StashAndClearLevels();
            return $"Sink '{sink}' disabled. Previous levels stashed for re-enable.";
        } else {
            route.RestoreStashedLevels();
            return $"Sink '{sink}' re-enabled with levels: [{string.Join(", ", route.Levels)}]";
        }
    }
}
