namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Abstraction for the sink plugin registry.
/// Enables dependency injection and testability.
/// </summary>
public interface ISinkPluginRegistry {
    /// <summary>
    /// Finds the first plugin that can handle the given sink name.
    /// </summary>
    ISinkPlugin? FindHandler(string sinkName);

    /// <summary>
    /// Returns all registered plugins.
    /// </summary>
    IReadOnlyCollection<ISinkPlugin> All { get; }
}
