using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Thread-safe registry for sink plugins.
/// Plugins self-register via [ModuleInitializer] using the static Register method.
/// Consumers should depend on <see cref="ISinkPluginRegistry"/> for testability.
/// </summary>
public static class SinkPluginRegistry {
    private static readonly DefaultSinkPluginRegistry _instance = new();

    /// <summary>
    /// Registers a sink plugin in the global registry.
    /// Called from plugin assemblies via [ModuleInitializer].
    /// </summary>
    public static void Register(ISinkPlugin plugin) => _instance.Register(plugin);

    /// <summary>
    /// Returns the singleton registry instance for DI registration.
    /// </summary>
    internal static ISinkPluginRegistry Instance => _instance;

    /// <summary>
    /// Returns all registered plugins.
    /// </summary>
    public static IReadOnlyCollection<ISinkPlugin> All => _instance.All;

    /// <summary>
    /// Finds the first plugin that can handle the given sink name.
    /// Prefer injecting <see cref="ISinkPluginRegistry"/> instead of calling this directly.
    /// </summary>
    public static ISinkPlugin? FindHandler(string sinkName) => _instance.FindHandler(sinkName);

    /// <summary>
    /// Clears all registered plugins. Used for testing.
    /// </summary>
    internal static void Clear() => _instance.Clear();
}

/// <summary>
/// Concrete implementation of <see cref="ISinkPluginRegistry"/>.
/// Wraps a ConcurrentBag for thread-safe plugin storage.
/// </summary>
internal sealed class DefaultSinkPluginRegistry : ISinkPluginRegistry {
    private readonly ConcurrentBag<ISinkPlugin> _plugins = [];

    internal void Register(ISinkPlugin plugin) => _plugins.Add(plugin);

    public ISinkPlugin? FindHandler(string sinkName) =>
        _plugins.FirstOrDefault(p => p.CanHandle(sinkName));

    public IReadOnlyCollection<ISinkPlugin> All => _plugins;

    internal void Clear() => _plugins.Clear();
}
