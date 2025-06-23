using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Tracing;
public static class LoggerTelemetryActivitySource {
    //TODO:"LoggerHelper" ??
    public static readonly ActivitySource Instance = new("LoggerHelper");
}