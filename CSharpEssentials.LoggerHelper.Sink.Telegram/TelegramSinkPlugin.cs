using Serilog;
using Serilog.Debugging;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Telegram;
internal class TelegramSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    public bool CanHandle(string sinkName) => sinkName == "Telegram";
    // Applies the MSSqlServer sink configuration to the LoggerConfiguration
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        if (serilogConfig.SerilogOption == null || serilogConfig.SerilogOption.TelegramOption == null) {
            Serilog.Debugging.SelfLog.WriteLine($"Configuration exception : section TelegramOption missing on Serilog:SerilogConfiguration:SerilogOption https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
            return;
        }
        try { 
        loggerConfig.WriteTo.Conditional(
                            evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                            wt => wt.Sink(new CustomTelegramSink(
                                serilogConfig?.SerilogOption?.TelegramOption?.Api_Key,
                                serilogConfig?.SerilogOption?.TelegramOption?.chatId,
                                new CustomTelegramSinkFormatter(),
                                serilogConfig?.SerilogOption?.TelegramOption?.ThrottleInterval ?? TimeSpan.FromSeconds(0)
                            )));
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
        SinkPluginRegistry.Register(new TelegramSinkPlugin());
    }
}