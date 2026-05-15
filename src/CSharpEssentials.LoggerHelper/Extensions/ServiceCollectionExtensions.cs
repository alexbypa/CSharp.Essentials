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
    /// Adds LoggerHelper as a Microsoft.Extensions.Logging provider on ILoggingBuilder,
    /// so that "Logging:LogLevel" filters in appsettings.json are respected.
    ///
    /// Example:
    ///   builder.Logging.AddLoggerHelper(builder.Configuration);
    /// </summary>
    public static ILoggingBuilder AddLoggerHelper(this ILoggingBuilder loggingBuilder, IConfiguration configuration) {
        loggingBuilder.Services.AddLoggerHelper(configuration);
        return loggingBuilder;
    }

    /// <summary>
    /// Adds LoggerHelper as a Microsoft.Extensions.Logging provider on ILoggingBuilder
    /// with fluent builder configuration.
    ///
    /// Example:
    ///   builder.Logging.AddLoggerHelper(b => b.WithApplicationName("MyApp").AddRoute("Console", LogEventLevel.Information));
    /// </summary>
    public static ILoggingBuilder AddLoggerHelper(this ILoggingBuilder loggingBuilder, Action<LoggerHelperBuilder> configure) {
        loggingBuilder.Services.AddLoggerHelper(configure);
        return loggingBuilder;
    }

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
        var discovery = new CompositePluginDiscovery(
            new CompileTimePluginDiscovery(),
            new FileSystemPluginDiscovery());

        var loadedSinkStore = new LoadedSinkStore();

        // Build eagerly to avoid circular resolution (ILoggerProvider ↔ Serilog.ILogger) during DI startup.
        var serilogLogger = LoggerPipelineFactory.Build(
            options, errorStore, loadedSinkStore, registry, discovery, customEnrichers, contextEnricher: null);

        // Wire legacy static API for backward compatibility
        LegacyLoggerHolder.Instance = serilogLogger;

        // Register services — consumers should depend on interfaces (DIP)
        services.AddSingleton(options);
        services.AddSingleton<ILogErrorStore>(errorStore);
        services.AddSingleton(errorStore); // backward compat: allow resolving concrete type
        services.AddSingleton<ILoadedSinkStore>(loadedSinkStore);
        services.AddSingleton(loadedSinkStore);
        services.AddSingleton<ISinkPluginRegistry>(registry);
        services.AddSingleton<Serilog.ILogger>(serilogLogger);
        services.AddSingleton<ILoggerProvider>(sp =>
            new LoggerHelperProvider(serilogLogger, sp.GetService<IContextLogEnricher>()));

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
        if (section.GetSection("Sinks").Exists())
            options.RawSinksSection = section.GetSection("Sinks");

        // Fallback: legacy Serilog:SerilogConfiguration (v2–v4 JSON)
        if (options.Routes.Count == 0)
            LegacyConfigurationAdapter.TryApply(finalConfig, options);

        if (options.Routes.Count == 0)
            throw new InvalidOperationException(
                $"No routes configured. Add 'LoggerHelper:Routes' to {fileName}, legacy 'Serilog:SerilogConfiguration', or use the fluent API. " +
                "See: https://github.com/alexbypa/CSharp.Essentials");

        return options;
    }
}
