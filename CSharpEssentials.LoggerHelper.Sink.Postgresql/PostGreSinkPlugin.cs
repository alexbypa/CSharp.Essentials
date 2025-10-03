using Serilog;
using Serilog.Debugging;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CSharpEssentials.LoggerHelper.Sink.Postgresql;
    internal class PostGreSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    public bool CanHandle(string sinkName) => sinkName == "PostgreSQL";
    // Applies the MSSqlServer sink configuration to the LoggerConfiguration
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        if (serilogConfig.SerilogOption == null || serilogConfig.SerilogOption.PostgreSQL == null) {
            SelfLog.WriteLine($"Configuration exception : section PostgreSQL missing on Serilog:SerilogConfiguration:SerilogOption https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
            return;
        }
        try {
        loggerConfig.WriteTo.Conditional(
                            evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                            wt => {
                                wt.PostgreSQL(
                                        connectionString: serilogConfig?.SerilogOption?.PostgreSQL?.connectionstring,
                                        tableName: serilogConfig.SerilogOption.PostgreSQL.tableName,
                                        schemaName: serilogConfig.SerilogOption.PostgreSQL.schemaName,
                                        needAutoCreateTable: true,
                                        columnOptions: CustomPostgresQLSink.BuildPostgresColumns(serilogConfig).GetAwaiter().GetResult()
                                    );
                                }
                            );
        }catch (Exception ex) {
            SelfLog.WriteLine($"Error HandleSink on PostgreSQL: {ex.Message}");
        }

    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new PostGreSinkPlugin());
    }
}