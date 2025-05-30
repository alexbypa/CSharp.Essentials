using Serilog;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sinks.Elasticsearch;
    public class ElasticSearchSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    public bool CanHandle(string sinkName) => sinkName == "ElasticSearch";
    // Applies the MSSqlServer sink configuration to the LoggerConfiguration
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        var opts = serilogConfig.SerilogOption.MSSqlServer;

        loggerConfig.WriteTo.Conditional(
            evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
            wt => {
                var elasticUrl = serilogConfig?.SerilogOption?.ElasticSearch?.nodeUris ?? "http://localhost:9200";
                try {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                    var response = client.GetAsync(elasticUrl).Result;
                    if (response.IsSuccessStatusCode) {
                        wt.Elasticsearch(
                            nodeUris: serilogConfig?.SerilogOption?.ElasticSearch?.nodeUris,
                            indexFormat: serilogConfig?.SerilogOption?.ElasticSearch?.indexFormat,
                            autoRegisterTemplate: true,
                            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
                            );
                    } else {
                        Serilog.Debugging.SelfLog.WriteLine($"[ElasticSearch] HTTP status not valid: {response.StatusCode}");
                    }
                } catch (Exception ex) {
                    Serilog.Debugging.SelfLog.WriteLine($"[ElasticSearch] Unreachable: {ex.Message}");
                }
            });
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new ElasticSearchSinkPlugin());
    }
}