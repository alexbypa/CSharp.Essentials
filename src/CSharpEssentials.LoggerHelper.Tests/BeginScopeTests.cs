using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class BeginScopeTests {
    [Fact]
    public void BeginScope_ReturnsDisposable() {
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

        Assert.NotNull(scope);
        logger.LogInformation("Scoped log");
    }
}
