using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RoutingOverheadBenchmark
{
    private Microsoft.Extensions.Logging.ILogger _singleRoute = null!;
    private Microsoft.Extensions.Logging.ILogger _multiRoute = null!;
    private ServiceProvider _sp1 = null!;
    private ServiceProvider _sp2 = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Single route
        var s1 = new ServiceCollection();
        s1.AddLoggerHelper(b => b
            .WithApplicationName("SingleRoute")
            .AddRoute("Console", LogEventLevel.Information)
        );
        _sp1 = s1.BuildServiceProvider();
        _singleRoute = _sp1.GetRequiredService<ILoggerProvider>().CreateLogger("Single");

        // Multiple routes (simulates real-world: console + file + remote)
        var s2 = new ServiceCollection();
        s2.AddLoggerHelper(b => b
            .WithApplicationName("MultiRoute")
            .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
            .AddRoute("File", LogEventLevel.Error, LogEventLevel.Fatal)
        );
        _sp2 = s2.BuildServiceProvider();
        _multiRoute = _sp2.GetRequiredService<ILoggerProvider>().CreateLogger("Multi");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _sp1?.Dispose();
        _sp2?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void SingleRoute_Info()
    {
        _singleRoute.LogInformation("Routing test {Value}", 1);
    }

    [Benchmark]
    public void MultiRoute_Info()
    {
        _multiRoute.LogInformation("Routing test {Value}", 1);
    }

    [Benchmark]
    public void MultiRoute_Error()
    {
        _multiRoute.LogError("Routing test {Value}", 1);
    }

    [Benchmark]
    public void MultiRoute_Debug_Filtered()
    {
        _multiRoute.LogDebug("Filtered {Value}", 1);
    }
}
