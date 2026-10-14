using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class BeginScopeTests {
    private readonly ITestOutputHelper _output;

    public BeginScopeTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void BeginScope_ReturnsDisposable() {
        _output.WriteLine("[Scenario] Logger con rotta Console: apro uno scope con BeginScope({OrderId=42}) e poi scrivo un log Information");
        _output.WriteLine("[Atteso] BeginScope restituisce un IDisposable non null e il log nello scope non lancia eccezioni");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLoggerHelper(b => b
            .WithApplicationName("ScopeTest")
            .DisableOpenTelemetry()
            .AddRoute("Console", LogEventLevel.Information));

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Scope");

        using var scope = logger.BeginScope(new Dictionary<string, object?> {
            ["OrderId"] = 42
        });

        _output.WriteLine($"[Restituito] scope={(scope is null ? "null" : scope.GetType().Name)}");
        Assert.NotNull(scope);
        logger.LogInformation("Scoped log");
    }
}
