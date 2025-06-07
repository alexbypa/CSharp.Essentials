using Serilog;
using System.Runtime.CompilerServices;
using Serilog.Formatting.Json;
using Microsoft.Extensions.DependencyInjection;
using CSharpEssentials.LoggerHelper.model;

namespace CSharpEssentials.LoggerHelper.Sink.File;
internal class FileSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    private readonly LoggerErrorStore _loggerErrorStore;

    public FileSinkPlugin(LoggerErrorStore loggerErrorStore) {
        _loggerErrorStore = loggerErrorStore;
    }
    public bool CanHandle(string sinkName) => sinkName == "File";
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        var logDirectory = serilogConfig?.SerilogOption?.File?.Path ?? "Logs";
        try {
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
        } catch (Exception ex) {
            _loggerErrorStore?.Add(new LogErrorEntry {
                SinkName = "File",
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                ContextInfo = $"Path: {logDirectory}"
            });
        }
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init(IServiceProvider provider) {
        var store = provider.GetRequiredService<LoggerErrorStore>();
        SinkPluginRegistry.Register(new FileSinkPlugin(store));
    }

}