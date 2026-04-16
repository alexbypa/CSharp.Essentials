using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog;
using Serilog.Debugging;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Core engine that configures Serilog routing using discovered sink plugins.
/// Depends on abstractions (ISinkPluginRegistry, IPluginDiscovery, ILogErrorStore)
/// instead of concrete/static classes — satisfying the Dependency Inversion Principle.
/// </summary>
internal sealed class SinkRoutingEngine {
    private readonly LoggerHelperOptions _options;
    private readonly ILogErrorStore _errorStore;
    private readonly ISinkPluginRegistry _registry;
    private readonly IPluginDiscovery _pluginDiscovery;

    internal SinkRoutingEngine(
        LoggerHelperOptions options,
        ILogErrorStore errorStore,
        ISinkPluginRegistry registry,
        IPluginDiscovery pluginDiscovery) {
        _options = options;
        _errorStore = errorStore;
        _registry = registry;
        _pluginDiscovery = pluginDiscovery;
    }

    /// <summary>
    /// Discovers plugins and configures each routing rule on the LoggerConfiguration.
    /// </summary>
    internal void ConfigureRoutes(LoggerConfiguration loggerConfig) {
        _pluginDiscovery.DiscoverAndLoad(_errorStore);

        foreach (var route in _options.Routes) {
            if (route.Levels.Count == 0)
                continue;

            var plugin = _registry.FindHandler(route.Sink);
            if (plugin is null) {
                var msg = $"No plugin found for sink '{route.Sink}'. Is the NuGet package CSharpEssentials.LoggerHelper.Sink.{route.Sink} installed?";
                SelfLog.WriteLine(msg);
                _errorStore.Add(new LogErrorEntry {
                    SinkName = route.Sink,
                    ErrorMessage = msg
                });
                continue;
            }

            try {
                plugin.Configure(loggerConfig, route, _options);
            } catch (Exception ex) {
                SelfLog.WriteLine($"Error configuring sink '{route.Sink}': {ex.Message}");
                _errorStore.Add(new LogErrorEntry {
                    SinkName = route.Sink,
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}
