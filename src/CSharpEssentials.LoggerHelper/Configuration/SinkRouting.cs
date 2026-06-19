using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Defines how log events are routed to a specific sink.
/// Maps a sink name to the log levels that should be forwarded to it,
/// with optional probabilistic sampling.
/// </summary>
public sealed class SinkRouting {
    /// <summary>
    /// The sink name (e.g., "Console", "File", "Email").
    /// </summary>
    public string Sink { get; set; } = string.Empty;

    /// <summary>
    /// The log levels to route to this sink (e.g., ["Error", "Fatal"]).
    /// Populated at startup from JSON config or fluent API; treat as read-only after the
    /// logger pipeline is built — mutating it after first <see cref="Matches"/> call has no effect.
    /// </summary>
    public List<string> Levels { get; set; } = [];

    /// <summary>
    /// Fraction of matching log events to forward to this sink (0.0–1.0).
    /// <c>null</c> or <c>1.0</c> means 100% (no sampling — every matching event is forwarded).
    /// <c>0.5</c> means ~50% of matching events reach the sink.
    /// Useful for reducing volume on expensive sinks (Elasticsearch, SQL) in high-throughput apps
    /// while keeping 100% of critical errors routed elsewhere.
    /// </summary>
    public double? SamplingRate { get; set; }

    // Lazily built from Levels on first Matches() call.
    // volatile: the null-check in Matches() is safe without a lock — worst case two threads
    // both build equivalent sets; the assignment of a reference type is atomic.
    private volatile HashSet<LogEventLevel>? _levelSet;

    /// <summary>
    /// Checks if the given log event level matches this routing rule.
    /// Called inside every Serilog Conditional predicate on the hot path — zero allocations.
    /// </summary>
    public bool Matches(LogEventLevel level) =>
        (_levelSet ?? BuildLevelSet()).Contains(level);

    /// <summary>
    /// Checks level match AND applies probabilistic sampling.
    /// When <see cref="SamplingRate"/> is <c>null</c> or <c>1.0</c>, identical to <see cref="Matches"/>
    /// (no random call, no overhead). Otherwise uses <see cref="Random.Shared"/> for thread-safe sampling.
    /// </summary>
    public bool ShouldEmit(LogEventLevel level) {
        if (!Matches(level))
            return false;
        var rate = SamplingRate;
        if (rate is null or >= 1.0)
            return true;
        if (rate <= 0.0)
            return false;
        return Random.Shared.NextDouble() < rate.Value;
    }

    private HashSet<LogEventLevel> BuildLevelSet() {
        var set = new HashSet<LogEventLevel>(Levels.Count);
        foreach (var l in Levels) {
            if (Enum.TryParse<LogEventLevel>(l, ignoreCase: true, out var ev))
                set.Add(ev);
        }
        _levelSet = set;
        return set;
    }
}