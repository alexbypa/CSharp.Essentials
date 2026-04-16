using CSharpEssentials.LoggerHelper.Diagnostics;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Abstraction for discovering and loading sink plugin assemblies.
/// Decouples the routing engine from the filesystem.
/// </summary>
internal interface IPluginDiscovery {
    /// <summary>
    /// Discovers and loads sink plugin assemblies, reporting errors to the store.
    /// </summary>
    void DiscoverAndLoad(ILogErrorStore errorStore);
}
