using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;
public static class SerilogConfigExtensions {
    public static bool IsSinkLevelMatch(this SerilogConfiguration config, string sink, LogEventLevel level) {
        var response = config?.SerilogCondition?.Any(c => c.Sink == sink && c.Level.Contains(level.ToString())) == true;
        return response;
    }
}