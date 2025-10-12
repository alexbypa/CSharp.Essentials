using Serilog;
using Serilog.Core;
using System.Runtime.CompilerServices;
using CSharpEssentials.LoggerHelper.InMemorySink;

namespace CSharpEssentials.LoggerHelper.Sink.Console;
internal class ConsoleSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) => sinkName == "Console";
    ILogEventSink dashboardSink = new InMemoryDashboardSink();
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        loggerConfig.WriteTo.Conditional(
            evt => serilogConfig.IsSinkLevelMatch(condition.Sink ?? "", evt.Level) && !evt.Properties.TryGetValue("TargetSink", out _),
            wt => wt.Console()
        ).WriteTo.Conditional(
            evt => serilogConfig.IsSinkLevelMatch(condition.Sink ?? "", evt.Level) && evt.Properties["TargetSink"] != null && evt.Properties["TargetSink"].ToString() == "\"Dashboard\"",
            wt => wt.Sink(dashboardSink)
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