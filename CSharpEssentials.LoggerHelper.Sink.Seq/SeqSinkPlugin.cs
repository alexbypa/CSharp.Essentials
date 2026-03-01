using Serilog;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Seq;
internal class SeqSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) => sinkName == "Seq";

    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        loggerConfig.WriteTo.Conditional(
            evt => serilogConfig.IsSinkLevelMatch(condition.Sink ?? "", evt.Level),
            //wt => wt.Console()
            wt => wt.Seq(serilogConfig.SerilogOption?.SeqOptions.serverUrl)
        );
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new SeqSinkPlugin());
    }
}