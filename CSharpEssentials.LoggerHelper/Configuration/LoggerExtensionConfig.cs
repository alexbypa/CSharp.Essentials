using CSharpEssentials.LoggerHelper.shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Serilog;
using Serilog.Events;
using System.Runtime.CompilerServices;

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

        builder.Configuration.AddJsonFile(SettingsProvider, fileNameSettings, false, true)
            .AddEnvironmentVariables();


        var configuration = builder.Configuration;
        services.Configure<SerilogConfiguration>(configuration.GetSection("Serilog:SerilogConfiguration"));
        var _serilogConfig = configuration.GetSection("Serilog:SerilogConfiguration").Get<SerilogConfiguration>();
        if (_serilogConfig == null)
            throw new InvalidOperationException($"Section 'Serilog:SerilogConfiguration' not found on {fileNameSettings}. See Documentation https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
        if (_serilogConfig.SerilogOption == null)
            throw new InvalidOperationException($"Section 'Serilog:SerilogConfiguration:SerilogOption' not found on {fileNameSettings}. See Documentation https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
        if (_serilogConfig.SerilogCondition == null)
            throw new InvalidOperationException($"Section 'Serilog:SerilogConfiguration:SerilogCondition' not found on {fileNameSettings}. See Documentation https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");

        var appName = _serilogConfig.ApplicationName;
        var _config = new LoggerConfiguration().ReadFrom.Configuration(configuration)
            .WriteTo.Sink(new OpenTelemetryLogEventSink())//TODO: da configurare
            .Enrich.WithProperty("ApplicationName", appName)
            .Enrich.FromLogContext()
            .Enrich.With<RenderedMessageEnricher>();

        services.AddSingleton<LoggerErrorStore>();

        LoggerConfigHelper.Initialize(configPath, _serilogConfig, _config);
        
        loggerExtension<RequestInfo>.TraceDashBoardSync(new RequestInfo { Action = "Setup" }, LogEventLevel.Warning, null, $"Using LoggerHelper settings from {configPath} with AddEnvironmentVariables !");
        
        ConfigurationPrinter.PrintByProvider(builder.Configuration);
        
        return services;
    }
#endif
}
public static class LoggerConfigHelper {
    public static string fileNameSettings { get; set; }
    public static SerilogConfiguration SerilogConfig { get; set; } 
    public static LoggerConfiguration LoggerConfig { get; set; }
    public static void Initialize(string _fileNameSettings, SerilogConfiguration _SerilogConfig, LoggerConfiguration _LoggerConfig) {
        fileNameSettings = _fileNameSettings;
        SerilogConfig = _SerilogConfig;
        LoggerConfig = _LoggerConfig;
    }
}
