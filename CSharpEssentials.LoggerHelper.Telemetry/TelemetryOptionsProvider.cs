using CSharpEssentials.LoggerHelper.Telemetry.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.Telemetry;
/// <summary>
/// Class for configuration
/// </summary>
public class TelemetryOptionsProvider {
    /// <summary>
    /// Load extensions
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static LoggerTelemetryOptions Load(WebApplicationBuilder builder) {
            var section = builder.Configuration.GetSection("Serilog:SerilogConfiguration:LoggerTelemetryOptions");
            // 1. Registre IOptionsMonitor<T>
            builder.Services.Configure<LoggerTelemetryOptions>(section);
            // 2. return objects
            return section.Get<LoggerTelemetryOptions>() ?? new();
    }
}
