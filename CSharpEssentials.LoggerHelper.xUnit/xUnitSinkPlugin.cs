using Serilog;
using Serilog.Debugging;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace CSharpEssentials.LoggerHelper.xUnit;
public class xUnitSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) => sinkName == "xUnit";

    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        try {
            loggerConfig.WriteTo.Conditional(
                evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                wt => wt.Sink(new customxUnitSink())
            );
        } catch (Exception ex) {
            SelfLog.WriteLine($"Error HandleSink on Telegram: {ex.Message}");
        }
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new xUnitSinkPlugin());
    }
}