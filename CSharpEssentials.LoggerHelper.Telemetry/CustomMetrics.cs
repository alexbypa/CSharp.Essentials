using System.Diagnostics.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public static class CustomMetrics {
    public static readonly Meter Meter = new("LoggerHelper.Metrics", "1.0");
    public static int CurrentSecond => DateTime.UtcNow.Second;

    public static readonly GaugeWrapper<int> CurrentSecondGauge =
                new(Meter, "current_second", () => DateTime.UtcNow.Second, "seconds", "Current second of the minute");

    public static readonly GaugeWrapper<double> MemoryUsedGauge =
        new(Meter, "memory_used_mb", () => {
            var bytes = GC.GetTotalMemory(false);
            return Math.Round(bytes / 1024.0 / 1024.0, 2);
        }, "MB", "Managed memory used (approx)");
}
