using Serilog;
using System.Runtime.CompilerServices;
using Serilog.Formatting.Json;

namespace CSharpEssentials.LoggerHelper.Sink.File;
    internal class FileSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    public bool CanHandle(string sinkName) => sinkName == "File";
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        if (serilogConfig.SerilogOption == null || serilogConfig.SerilogOption.File == null) {
            Serilog.Debugging.SelfLog.WriteLine($"Configuration exception : section file missing on Serilog:SerilogConfiguration:SerilogOption https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
            return;
        }
        var logDirectory = serilogConfig?.SerilogOption?.File?.Path ?? "Logs";
        var logFilePath = Path.Combine(logDirectory, "log-.txt");
        Directory.CreateDirectory(logDirectory);
        
        loggerConfig.WriteTo.Conditional(
            evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
            wt => wt.File(
                new JsonFormatter(),
                logFilePath,
                rollingInterval: Enum.Parse<RollingInterval>(serilogConfig?.SerilogOption?.File?.RollingInterval ?? "Day"),
                retainedFileCountLimit: serilogConfig?.SerilogOption?.File?.RetainedFileCountLimit ?? 7,
                shared: serilogConfig?.SerilogOption?.File?.Shared ?? true
                )
            );
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new FileSinkPlugin());
    }
}