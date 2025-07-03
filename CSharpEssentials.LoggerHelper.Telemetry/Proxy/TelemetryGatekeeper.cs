using CSharpEssentials.LoggerHelper.Configuration;
using CSharpEssentials.LoggerHelper.Telemetry.Configuration;
using Microsoft.Extensions.Options;

namespace CSharpEssentials.LoggerHelper.Telemetry.Proxy;
/// <summary>
/// Acts as a gatekeeper to determine if telemetry is enabled based on runtime configuration.
/// Listens for changes in LoggerTelemetryOptions using IOptionsMonitor.
/// </summary>
public class TelemetryGatekeeper : ITelemetryGatekeeper {
private readonly IOptionsMonitor<LoggerTelemetryOptions> _options;
    /// <summary>
    /// Constructor that sets up the gatekeeper and listens for config changes.
    /// </summary>
    /// <param name="options">Options monitor for LoggerTelemetryOptions</param>
    /// <param name="loggerConfigInfo">Injected but not used directly here (may be used elsewhere)</param>
    public TelemetryGatekeeper(IOptionsMonitor<LoggerTelemetryOptions> options, ILoggerConfigInfo loggerConfigInfo) {
        _options = options;
        _options.OnChange(options => {
            Console.WriteLine($"[CHANGE DETECTED] IsEnabled = {options.IsEnabled}");
        });
    }
    /// <summary>
    /// Exposes whether telemetry is currently enabled, based on the latest configuration.
    /// </summary>
    public bool IsEnabled => _options.CurrentValue?.IsEnabled ?? false;
}