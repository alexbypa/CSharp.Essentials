namespace CSharpEssentials.LoggerHelper.Telemetry.Proxy;
/// <summary>
/// Acts as a gatekeeper to determine if telemetry is enabled based on runtime configuration.
/// Listens for changes in LoggerTelemetryOptions using IOptionsMonitor.
/// </summary>
public interface ITelemetryGatekeeper {
    bool IsEnabled { get; }
}