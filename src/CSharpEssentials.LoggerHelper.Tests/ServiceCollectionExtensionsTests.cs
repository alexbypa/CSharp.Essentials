using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class ServiceCollectionExtensionsTests {
    [Fact]
    public void AddLoggerHelper_Fluent_RegistersServices() {
        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("TestApp")
            .AddRoute("Console", LogEventLevel.Information)
        );

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<LoggerHelperOptions>());
        Assert.NotNull(provider.GetService<ILogErrorStore>());
        Assert.NotNull(provider.GetService<LogErrorStore>());
        Assert.NotNull(provider.GetService<ISinkPluginRegistry>());
        Assert.NotNull(provider.GetService<Serilog.ILogger>());
        Assert.NotNull(provider.GetService<ILoggerProvider>());
    }

    [Fact]
    public void AddLoggerHelper_SetsApplicationName() {
        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("MyTestApp")
            .AddRoute("Console", LogEventLevel.Information)
        );

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();

        Assert.Equal("MyTestApp", options.ApplicationName);
    }

    [Fact]
    public void AddLoggerHelper_ILoggerProvider_CreatesLoggers() {
        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("ILoggerTest")
            .AddRoute("Console", LogEventLevel.Information)
        );

        var provider = services.BuildServiceProvider();
        var loggerProvider = provider.GetRequiredService<ILoggerProvider>();
        var logger = loggerProvider.CreateLogger("TestCategory");

        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void AddLoggerHelper_WithMultipleRoutes_ConfiguresAll() {
        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("MultiRoute")
            .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
            .AddRoute("File", LogEventLevel.Error, LogEventLevel.Fatal)
        );

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();

        Assert.Equal(2, options.Routes.Count);
        Assert.Equal("Console", options.Routes[0].Sink);
        Assert.Equal("File", options.Routes[1].Sink);
    }

    [Fact]
    public void AddLoggerHelper_EnableSelfLogging_SetsFlag() {
        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("SelfLogTest")
            .AddRoute("Console", LogEventLevel.Information)
            .EnableSelfLogging()
        );

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();

        Assert.True(options.General.EnableSelfLogging);
    }

    [Fact]
    public void AddLoggerHelper_ErrorStore_IsRegistered() {
        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("ErrorStoreTest")
            .AddRoute("Console", LogEventLevel.Information)
        );

        var provider = services.BuildServiceProvider();

        // Both interface and concrete type should resolve
        var errorStore = provider.GetRequiredService<ILogErrorStore>();
        var concreteStore = provider.GetRequiredService<LogErrorStore>();

        Assert.NotNull(errorStore);
        Assert.NotNull(concreteStore);
    }
}
