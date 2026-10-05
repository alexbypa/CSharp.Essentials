namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Describes the configuration outcome of a sink route at startup (see <see cref="Configured"/>).
/// </summary>
public sealed class LoadedSinkInfo {
    public string SinkName { get; init; } = string.Empty;
    public string PluginType { get; init; } = string.Empty;
    public IReadOnlyList<string> Levels { get; init; } = [];
    /// <summary>True if the sink was configured successfully; false if its configuration failed.</summary>
    public bool Configured { get; init; }
}

/// <summary>
/// Read-only view of the configuration outcome of each sink route (configured or failed).
/// </summary>
public interface ILoadedSinkStore {
    IReadOnlyList<LoadedSinkInfo> GetAll();
}

/// <summary>
/// Thread-safe store for the configuration outcome of sink routes.
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
