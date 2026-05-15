namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Describes a sink route that was configured successfully at startup.
/// </summary>
public sealed class LoadedSinkInfo {
    public string SinkName { get; init; } = string.Empty;
    public string PluginType { get; init; } = string.Empty;
    public IReadOnlyList<string> Levels { get; init; } = [];
    public bool Configured { get; init; }
}

/// <summary>
/// Read-only view of successfully loaded sink routes.
/// </summary>
public interface ILoadedSinkStore {
    IReadOnlyList<LoadedSinkInfo> GetAll();
}

/// <summary>
/// Thread-safe store for successfully configured sinks.
/// </summary>
public sealed class LoadedSinkStore : ILoadedSinkStore {
    private readonly List<LoadedSinkInfo> _entries = [];

    internal void Add(LoadedSinkInfo entry) {
        lock (_entries) {
            _entries.Add(entry);
        }
    }

    public IReadOnlyList<LoadedSinkInfo> GetAll() {
        lock (_entries) {
            return _entries.ToList();
        }
    }

    internal void Clear() {
        lock (_entries) {
            _entries.Clear();
        }
    }
}
