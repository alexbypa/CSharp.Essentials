using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LoggerHelperProviderTests {
    private readonly ITestOutputHelper _output;

    public LoggerHelperProviderTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void LogInformation_WritesThroughProvider() {
        _output.WriteLine("[Scenario] AddLoggerHelper con rotta Console/Information e OTel disabilitato: ILogger.LogInformation passa dal provider LoggerHelper");
        _output.WriteLine("[Atteso] Nessuna eccezione: il bridge ILogger -> Serilog funziona");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLoggerHelper(b => b
            .WithApplicationName("ProviderTest")
            .DisableOpenTelemetry()
            .AddRoute("Console", LogEventLevel.Information));

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Test");
        logger.LogInformation("Provider bridge works");
        _output.WriteLine($"[Restituito] LogInformation completato senza eccezioni");
    }

    [Fact]
    public void LoggingBuilder_AddLoggerHelper_RegistersProvider() {
        _output.WriteLine("[Scenario] ILoggingBuilder.AddLoggerHelper (dopo ClearProviders) con rotta Console/Warning: risolvo i ILoggerProvider registrati");
        _output.WriteLine("[Atteso] Tra i provider esiste un LoggerHelperProvider");

        var services = new ServiceCollection();
        services.AddLogging(logging => logging
            .ClearProviders()
            .AddLoggerHelper(b => b
                .WithApplicationName("LoggingBuilderTest")
                .DisableOpenTelemetry()
                .AddRoute("Console", LogEventLevel.Warning)));

        using var sp = services.BuildServiceProvider();
        var providers = sp.GetServices<ILoggerProvider>().ToList();
        _output.WriteLine($"[Restituito] Providers=[{string.Join(", ", providers.Select(p => p.GetType().Name))}]");
        Assert.Contains(providers, p => p is LoggerHelperProvider);
    }
}
