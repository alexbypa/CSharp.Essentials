using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Extension methods for registering LoggerHelper in the DI container.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds LoggerHelper with fluent builder configuration.
    ///
    /// Example:
    ///   builder.Services.AddLoggerHelper(b => b
    ///       .WithApplicationName("MyApp")
    ///       .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
    ///   );
    /// </summary>
    public static IServiceCollection AddLoggerHelper(this IServiceCollection services, Action<LoggerHelperBuilder> configure) {
        var builder = new LoggerHelperBuilder();
        configure(builder);
        return services.AddLoggerHelperCore(builder.Options, builder.CustomEnrichers);
    }

    /// <summary>
    /// Adds LoggerHelper with JSON configuration from appsettings.LoggerHelper.json.
    ///
    /// Example:
    ///   builder.Services.AddLoggerHelper(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddLoggerHelper(this IServiceCollection services, IConfiguration configuration) {
        var options = ResolveOptionsFromJson(configuration);
        return services.AddLoggerHelperCore(options, customEnrichers: null);
    }

    /// <summary>
    /// Adds LoggerHelper with both JSON configuration and fluent overrides.
    ///
    /// Example:
    ///   builder.Services.AddLoggerHelper(builder.Configuration, b => b
    ///       .AddRoute("Console", LogEventLevel.Debug)  // add extra route beyond JSON
    ///   );
    /// </summary>
    public static IServiceCollection AddLoggerHelper(this IServiceCollection services, IConfiguration configuration, Action<LoggerHelperBuilder> configure) {
        var options = ResolveOptionsFromJson(configuration);

        var builder = new LoggerHelperBuilder();
        configure(builder);

        // Merge: fluent routes are additive, fluent application name overrides JSON
        options.Routes.AddRange(builder.Options.Routes);
        if (!string.IsNullOrEmpty(builder.Options.ApplicationName))
            options.ApplicationName = builder.Options.ApplicationName;

        return services.AddLoggerHelperCore(options, builder.CustomEnrichers);
    }

    private static IServiceCollection AddLoggerHelperCore(this IServiceCollection services, LoggerHelperOptions options, Action<Serilog.LoggerConfiguration>? customEnrichers) {
        var errorStore = new LogErrorStore();
        var registry = SinkPluginRegistry.Instance;
        var discovery = new FileSystemPluginDiscovery();

        var serilogLogger = LoggerPipelineFactory.Build(options, errorStore, registry, discovery, customEnrichers);

        // Register services — consumers should depend on interfaces (DIP)
        services.AddSingleton(options);
        services.AddSingleton<ILogErrorStore>(errorStore);
        services.AddSingleton(errorStore); // backward compat: allow resolving concrete type
        services.AddSingleton<ISinkPluginRegistry>(registry);
        services.AddSingleton<Serilog.ILogger>(serilogLogger);
        services.AddSingleton<ILoggerProvider>(new LoggerHelperProvider(serilogLogger));

        return services;
    }

    private static LoggerHelperOptions ResolveOptionsFromJson(IConfiguration configuration) {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        var fileName = string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)
            ? "appsettings.LoggerHelper.debug.json"
            : "appsettings.LoggerHelper.json";

        var configPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        IConfiguration finalConfig;
        if (File.Exists(configPath)) {
            finalConfig = new ConfigurationBuilder()
                .AddJsonFile(new PhysicalFileProvider(Directory.GetCurrentDirectory()), fileName, optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        } else {
            finalConfig = configuration;
        }

        var section = finalConfig.GetSection("LoggerHelper");
        var options = new LoggerHelperOptions();
        section.Bind(options);

        // Store raw "Sinks" section for plugin-side JSON binding (OCP)
        options.RawSinksSection = section.GetSection("Sinks");

        if (options.Routes.Count == 0)
            throw new InvalidOperationException(
                $"No routes configured in LoggerHelper. Add a 'LoggerHelper:Routes' section to {fileName} or use the fluent API. " +
                "See: https://github.com/alexbypa/CSharp.Essentials");

        return options;
    }
}
