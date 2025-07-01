using CSharpEssentials.LoggerHelper.Telemetry.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public class TelemetryOptionsProvider {
    public static LoggerTelemetryOptions Load(WebApplicationBuilder builder) {
        return builder.Configuration.GetSection("Serilog:SerilogConfiguration:LoggerTelemetryOptions").Get<LoggerTelemetryOptions>() ?? new();
    }
}
