using CSharpEssentials.LoggerHelper.InMemorySink;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Console;
internal class ConsoleSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) => sinkName == "Console";
    ILogEventSink dashboardSink = new InMemoryDashboardSink();
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        loggerConfig.WriteTo.Conditional(
            evt => serilogConfig.IsSinkLevelMatch(condition.Sink ?? "", evt.Level),
            //wt => wt.Console()
            wt => wt.Sink(new CustomConsoleSink(null))
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

// Il Sink deve implementare l'interfaccia ILogEventSink
public class CustomConsoleSink : ILogEventSink {
    private readonly IFormatProvider _formatProvider;

    public CustomConsoleSink(IFormatProvider formatProvider) {
        _formatProvider = formatProvider;
    }
    public void Emit(LogEvent logEvent) {
        System.Console.ForegroundColor = GetColor(logEvent.Level);
        var message = logEvent.RenderMessage(_formatProvider);
        //System.Console.WriteLine(message);
        string exceptionMessage = logEvent.Exception != null ? logEvent.Exception.ToString() : "";  
        var formattedLog = $"[{logEvent.Timestamp.ToLocalTime().ToString("HH:mm:ss")} {logEvent.Level}] {message} {exceptionMessage}";
        System.Console.WriteLine(formattedLog);
        System.Console.ResetColor();
    }
    private static ConsoleColor GetColor(LogEventLevel level) {
        return level switch {
            LogEventLevel.Verbose => ConsoleColor.DarkGray,
            LogEventLevel.Debug => ConsoleColor.Gray,
            LogEventLevel.Information => ConsoleColor.Blue,    // **BLU** come richiesto
            LogEventLevel.Warning => ConsoleColor.DarkYellow,  // **ARANCIONE** (usiamo Yellow come simile)
            LogEventLevel.Error => ConsoleColor.Red,
            LogEventLevel.Fatal => ConsoleColor.DarkRed,
            _ => ConsoleColor.White,
        };
    }
}