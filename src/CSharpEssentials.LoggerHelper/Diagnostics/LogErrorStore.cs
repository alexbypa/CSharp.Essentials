using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Thread-safe, bounded store for LoggerHelper internal errors.
/// Uses a circular buffer: when full, oldest entries are dropped.
/// Prevents unbounded memory growth when a sink is misconfigured or down.
/// </summary>
public sealed class LogErrorStore : ILogErrorStore {
    private readonly ConcurrentQueue<LogErrorEntry> _errors = new();

    /// <summary>
    /// Maximum number of error entries to retain. Default: 1000.
    /// </summary>
    public int MaxCapacity { get; init; } = 1000;

    public void Add(LogErrorEntry entry) {
        _errors.Enqueue(entry);

        // Evict oldest entries if over capacity
        while (_errors.Count > MaxCapacity)
            _errors.TryDequeue(out _);
    }

    public IReadOnlyList<LogErrorEntry> GetAll() => [.. _errors];

    public void Clear() {
        while (_errors.TryDequeue(out _)) { }
    }

    public int Count => _errors.Count;
}
