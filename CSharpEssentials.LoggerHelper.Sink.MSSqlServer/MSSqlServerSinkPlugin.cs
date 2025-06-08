using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.MSSqlServer;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.MSSqlServer;
// Plugin implementation for handling the MSSqlServer sink
public class MSSqlServerSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    public bool CanHandle(string sinkName) => sinkName == "MSSqlServer";
    // Applies the MSSqlServer sink configuration to the LoggerConfiguration
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        if (serilogConfig.SerilogOption == null || serilogConfig.SerilogOption.PostgreSQL == null) {
            SelfLog.WriteLine($"Configuration exception : section MSSqlServer missing on Serilog:SerilogConfiguration:SerilogOption https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
            return;
        }
        var opts = serilogConfig.SerilogOption.MSSqlServer;
        loggerConfig.WriteTo.Conditional(
            evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
            wt => wt.MSSqlServer(serilogConfig?.SerilogOption?.MSSqlServer?.connectionString,
            new MSSqlServerSinkOptions {
                TableName = serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.tableName,
                SchemaName = serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.schemaName,
                AutoCreateSqlTable = serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.autoCreateSqlTable ?? false,
                BatchPostingLimit = serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.batchPostingLimit ?? 100,
                BatchPeriod = string.IsNullOrEmpty(serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.period) ? TimeSpan.FromSeconds(10) : TimeSpan.Parse(serilogConfig.SerilogOption.MSSqlServer.sinkOptionsSection.period),
            },
            columnOptions: CustomMSSQLServerSink.GetColumnsOptions_v2(serilogConfig?.SerilogOption.MSSqlServer)
        ));
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new MSSqlServerSinkPlugin());
    }
}