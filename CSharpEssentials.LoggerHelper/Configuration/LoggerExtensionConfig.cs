using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace CSharpEssentials.LoggerHelper.Configuration;
/// <summary>
/// Helper class for configuring and writing logs with Serilog.
/// </summary>
public static class LoggerExtensionConfig {
#if NET6_0
    /// <summary>
    /// Adds external LoggerHelper configuration (e.g., appsettings.LoggerHelper.json) to a WebApplicationBuilder.
    /// </summary>
    public static IServiceCollection AddLoggerConfiguration(this WebApplicationBuilder builder) {
        var externalConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.LoggerHelper.json");
        if (File.Exists(externalConfigPath)) {
            builder.Configuration.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);
        }
        builder.Services.AddSingleton<LoggerErrorStore>();
        return builder.Services;
    }
#else
    /// <summary>
    /// Adds external LoggerHelper configuration (e.g., appsettings.LoggerHelper.json) to a WebApplicationBuilder.
    /// </summary>
    public static IServiceCollection AddloggerConfiguration(this IServiceCollection services, WebApplicationBuilder builder) {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                       ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        var fileNameSettings = envName?.Equals("Development", StringComparison.OrdinalIgnoreCase)
                   == true
                   ? "appsettings.LoggerHelper.debug.json"
                   : "appsettings.LoggerHelper.json";

        var configPath = Path.Combine(Directory.GetCurrentDirectory(), fileNameSettings);
        var SettingsProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());


        builder.Configuration.AddJsonFile(SettingsProvider, fileNameSettings, false, true);

        services.AddSingleton<LoggerErrorStore>();
        services.AddSingleton<ILoggerConfigInfo, LoggerConfigInfo>((sp) => {
            return new LoggerConfigInfo { fileNameSettings = configPath };
        });

        return services;
    }
#endif
}
public class LoggerConfigInfo : ILoggerConfigInfo {
    public string fileNameSettings { get; set; }
}
public interface ILoggerConfigInfo {
    string fileNameSettings { get; set; }
}