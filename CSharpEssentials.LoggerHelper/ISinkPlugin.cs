using Serilog;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper;
// un “contratto” che ogni sink esterno dovrà implementare
public interface ISinkPlugin {
    /// <summary>
    /// Ritorna true se questo plugin sa gestire il sink di nome sinkName
    /// </summary>
    bool CanHandle(string sinkName);

    /// <summary>
    /// Qui il plugin registra effettivamente il sink sulla LoggerConfiguration
    /// </summary>
    void HandleSink(
        LoggerConfiguration loggerConfig,
        SerilogCondition condition,
        SerilogConfiguration serilogConfig);
}

// un registry globale in cui i plugin si auto-registrano
public static class SinkPluginRegistry {
    static SinkPluginRegistry() => Plugins = new List<ISinkPlugin>();
    private static List<ISinkPlugin> Plugins { get; }
    public static void Register(ISinkPlugin plugin) => Plugins.Add(plugin);
    public static IEnumerable<ISinkPlugin> All => Plugins;
}