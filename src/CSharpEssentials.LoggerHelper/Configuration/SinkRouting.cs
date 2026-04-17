using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Defines how log events are routed to a specific sink.
/// Maps a sink name to the log levels that should be forwarded to it.
/// </summary>
public sealed class SinkRouting {
    /// <summary>
    /// The sink name (e.g., "Console", "File", "Email").
    /// </summary>
    public string Sink { get; set; } = string.Empty;

    /// <summary>
    /// The log levels to route to this sink (e.g., ["Error", "Fatal"]).
    /// </summary>
    public List<string> Levels { get; set; } = [];

    /// <summary>
    /// Checks if the given log event level matches this routing rule.
    /// </summary>
    public bool Matches(LogEventLevel level) =>
        Levels.Contains(level.ToString(), StringComparer.OrdinalIgnoreCase);
}
