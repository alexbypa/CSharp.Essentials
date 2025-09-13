//TODO: IOptionsMonitor must be iomplemnented from appSettings.json...

//using CSharpEssentials.LoggerHelper.Telemetry.Configuration;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;

//namespace CSharpEssentials.LoggerHelper.Telemetry;
///// <summary>
///// Class for configuration
///// </summary>
//public class TelemetryOptionsProvider {
//    /// <summary>
//    /// Load extensions
//    /// </summary>
//    /// <param name="builder"></param>
//    /// <returns></returns>
//    public static LoggerTelemetryOptions Load(WebApplicationBuilder builder) {
//        builder.Services.AddSingleton<IOptionsMonitor<LoggerTelemetryOptions>>(sp => new DbOptionsMonitor<LoggerTelemetryOptions>(sp));
//        using var scope = builder.Services.BuildServiceProvider().CreateScope();
//        var optionsMonitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<LoggerTelemetryOptions>>();
//        return optionsMonitor.CurrentValue ?? new LoggerTelemetryOptions();
//    }
//}
