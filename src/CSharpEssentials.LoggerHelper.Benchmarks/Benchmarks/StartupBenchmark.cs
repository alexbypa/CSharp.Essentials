using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Targets;
using Serilog;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Costo di inizializzazione — rilevante per Azure Functions e cold start lambda.
/// Ogni invocazione crea e distrugge un'istanza del logger.
/// LoggerHelper disabilita OpenTelemetry per un confronto equo del solo costo DI/Serilog.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StartupBenchmark
{
    [Benchmark(Baseline = true)]
    public void Serilog_Startup()
    {
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(new NullSink())
            .CreateLogger();
    }

    [Benchmark]
    public void NLog_Startup()
    {
        var config = new LoggingConfiguration();
        var nullTarget = new NullTarget("null");
        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, nullTarget);
        var factory = new NLog.LogFactory { Configuration = config };
        _ = factory.GetLogger("StartupBench");
        factory.Shutdown();
    }

    [Benchmark]
    public void LoggerHelper_Startup()
    {
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("StartupBench")
            .DisableOpenTelemetry()
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));
        using var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<ILoggerProvider>().CreateLogger("StartupBench");
    }
}
