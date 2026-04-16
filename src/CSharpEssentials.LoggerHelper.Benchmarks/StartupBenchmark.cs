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
public class StartupBenchmark
{
    [Benchmark(Baseline = true)]
    public Serilog.ILogger RawSerilog_Startup()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(new NullSink())
            .CreateLogger();
    }

    [Benchmark]
    public ServiceProvider LoggerHelper_Startup()
    {
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("StartupBench")
            .AddRoute("Console", LogEventLevel.Information)
        );
        var sp = services.BuildServiceProvider();
        // Force resolution to measure full pipeline
        _ = sp.GetRequiredService<ILoggerProvider>();
        return sp;
    }
}
