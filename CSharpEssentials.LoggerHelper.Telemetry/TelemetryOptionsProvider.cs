using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public class TelemetryOptionsProvider {
    public static LoggerTelemetryOptions Load(WebApplicationBuilder builder) {
        var config = new ConfigurationBuilder()
#if DEBUG
            .AddJsonFile("appsettings.LoggerHelper.debug.json")
#else
            .AddJsonFile("appsettings.LoggerHelper.json")
#endif
            .Build();

        return config
            .GetSection("Serilog:SerilogConfiguration:LoggerTelemetryOptions")
            .Get<LoggerTelemetryOptions>() ?? new();
    }
}
