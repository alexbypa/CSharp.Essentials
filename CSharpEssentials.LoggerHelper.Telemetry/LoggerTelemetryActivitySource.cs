using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public static class LoggerTelemetryActivitySource {
    //TODO:"LoggerHelper" ??
    public static readonly ActivitySource Instance = new("LoggerHelper");
}