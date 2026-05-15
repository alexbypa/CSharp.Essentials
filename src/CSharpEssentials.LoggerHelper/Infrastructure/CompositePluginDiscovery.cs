using CSharpEssentials.LoggerHelper.Diagnostics;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Runs compile-time (source-generated) discovery first, then filesystem/reflection fallback.
/// </summary>
internal sealed class CompositePluginDiscovery : IPluginDiscovery {
    private readonly IPluginDiscovery _primary;
    private readonly IPluginDiscovery _fallback;

    internal CompositePluginDiscovery(IPluginDiscovery primary, IPluginDiscovery fallback) {
        _primary = primary;
        _fallback = fallback;
    }

    public void DiscoverAndLoad(ILogErrorStore errorStore) {
        _primary.DiscoverAndLoad(errorStore);
        _fallback.DiscoverAndLoad(errorStore);
    }
}
