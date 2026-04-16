using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Thread-safe store for LoggerHelper internal errors.
/// Registered as a singleton in DI for diagnostic access.
/// </summary>
public sealed class LogErrorStore : ILogErrorStore {
    private readonly ConcurrentQueue<LogErrorEntry> _errors = new();

    public void Add(LogErrorEntry entry) => _errors.Enqueue(entry);

    public IReadOnlyList<LogErrorEntry> GetAll() => [.. _errors];

    public void Clear() {
        while (_errors.TryDequeue(out _)) { }
    }

    public int Count => _errors.Count;
}
