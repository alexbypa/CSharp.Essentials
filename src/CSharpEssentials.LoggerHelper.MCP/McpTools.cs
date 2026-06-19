using CSharpEssentials.LoggerHelper.Diagnostics;
using System.Text;

namespace CSharpEssentials.LoggerHelper.MCP;

/// <summary>
/// Implements the four MCP tools that expose LoggerHelper diagnostics to AI assistants.
/// Register via <see cref="McpExtensions.AddLoggerHelperMcp"/> and call from POST /mcp.
/// </summary>
public sealed class LoggerHelperMcpTools {
    private readonly ILogErrorStore _errorStore;
    private readonly ILoadedSinkStore _sinkStore;
    private readonly LoggerHelperOptions _options;

    public LoggerHelperMcpTools(ILogErrorStore errorStore, ILoadedSinkStore sinkStore, LoggerHelperOptions options) {
        _errorStore = errorStore;
        _sinkStore  = sinkStore;
        _options    = options;
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

        var routeLookup = _options.Routes.ToDictionary(r => r.Sink, r => r, StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        sb.AppendLine($"Loaded LoggerHelper sinks ({sinks.Count}):");
        foreach (var s in sinks) {
            var status = s.Configured ? "ACTIVE" : "FAILED";
            var sampling = routeLookup.TryGetValue(s.SinkName, out var route) && route.SamplingRate is not null and < 1.0
                ? $" | Sampling: {route.SamplingRate:P0}" : "";
            sb.AppendLine($"  [{status}] {s.SinkName} | Levels: [{string.Join(", ", s.Levels)}]{sampling}");
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
        sb.AppendLine($"  Routes ({_options.Routes.Count}):");
        foreach (var r in _options.Routes) {
            var sampling = r.SamplingRate is null or >= 1.0 ? "" : $" | Sampling: {r.SamplingRate:P0}";
            sb.AppendLine($"    -> {r.Sink}: [{string.Join(", ", r.Levels)}]{sampling}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns the overall health status: OK / WARNING / CRITICAL.
    /// Tool name: <c>loggerhelper_get_health</c>
    /// </summary>
    public string GetHealth() {
        var errorCount    = _errorStore.Count;
        var sinks         = _sinkStore.GetAll();
        var activeCount   = sinks.Count(s => s.Configured);
        var status        = errorCount == 0 ? "OK" : errorCount < 10 ? "WARNING" : "CRITICAL";

        return $"""
            LoggerHelper Health
              Status  : {status}
              Sinks   : {activeCount} active / {sinks.Count} configured
              Errors  : {errorCount} recorded
            """;
    }
}
