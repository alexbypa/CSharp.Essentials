using Serilog;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Console;
    internal class ConsoleSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    public bool CanHandle(string sinkName) => sinkName == "Console";
    // Applies the MSSqlServer sink configuration to the LoggerConfiguration
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        var opts = serilogConfig.SerilogOption.MSSqlServer;
        loggerConfig.WriteTo.Conditional(
            evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
            wt => wt.Console()
        );
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new ConsoleSinkPlugin());
    }
}