using Elasticsearch.Net;
using Serilog.Events;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace CSharpEssentials.LoggerHelper;
public static class SerilogConfigExtensions {
    public static bool IsSinkEnabled(this SerilogConfiguration config, string sink)
        => config?.SerilogCondition?.Any(c => c.Sink == sink && c.Level?.Any() == true) == true;

    public static bool IsSinkLevelMatch(this SerilogConfiguration config, string sink, LogEventLevel level) {
        var response = config?.SerilogCondition?.Any(c => c.Sink == sink && c.Level.Contains(level.ToString())) == true;
        return response;
    }
}