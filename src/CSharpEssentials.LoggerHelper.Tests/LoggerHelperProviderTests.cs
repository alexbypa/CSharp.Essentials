using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LoggerHelperProviderTests {
    [Fact]
    public void LogInformation_WritesThroughProvider() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLoggerHelper(b => b
            .WithApplicationName("ProviderTest")
            .DisableOpenTelemetry()
            .AddRoute("Console", LogEventLevel.Information));

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Test");
        logger.LogInformation("Provider bridge works");
    }

    [Fact]
    public void LoggingBuilder_AddLoggerHelper_RegistersProvider() {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging
            .ClearProviders()
            .AddLoggerHelper(b => b
                .WithApplicationName("LoggingBuilderTest")
                .DisableOpenTelemetry()
                .AddRoute("Console", LogEventLevel.Warning)));

        using var sp = services.BuildServiceProvider();
        var providers = sp.GetServices<ILoggerProvider>().ToList();
        Assert.Contains(providers, p => p is LoggerHelperProvider);
    }
}
