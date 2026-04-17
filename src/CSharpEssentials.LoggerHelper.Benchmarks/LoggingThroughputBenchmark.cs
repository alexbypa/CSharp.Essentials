using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Benchmarks.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class LoggingThroughputBenchmark
{
    private Serilog.ILogger _rawSerilog = null!;
    private Microsoft.Extensions.Logging.ILogger _loggerHelper = null!;
    private ServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Raw Serilog — console sink, same level
        _rawSerilog = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(new NullSink())
            .CreateLogger();

        // LoggerHelper v5 — console route, same level
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("Benchmark")
            .AddRoute("Console", LogEventLevel.Information)
        );
        _serviceProvider = services.BuildServiceProvider();
        var loggerProvider = _serviceProvider.GetRequiredService<ILoggerProvider>();
        _loggerHelper = loggerProvider.CreateLogger("BenchmarkCategory");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void RawSerilog_SingleMessage()
    {
        _rawSerilog.Information("Benchmark message {Counter}", 42);
    }

    [Benchmark]
    public void LoggerHelper_SingleMessage()
    {
        _loggerHelper.LogInformation("Benchmark message {Counter}", 42);
    }

    [Benchmark]
    public void RawSerilog_100Messages()
    {
        for (int i = 0; i < 100; i++)
            _rawSerilog.Information("Batch message {Index}", i);
    }

    [Benchmark]
    public void LoggerHelper_100Messages()
    {
        for (int i = 0; i < 100; i++)
            _loggerHelper.LogInformation("Batch message {Index}", i);
    }

    [Benchmark]
    public void RawSerilog_StructuredPayload()
    {
        _rawSerilog.Information("Order {OrderId} for {Customer} total {Amount:C}",
            12345, "Acme Corp", 99.99m);
    }

    [Benchmark]
    public void LoggerHelper_StructuredPayload()
    {
        _loggerHelper.LogInformation("Order {OrderId} for {Customer} total {Amount:C}",
            12345, "Acme Corp", 99.99m);
    }

    [Benchmark]
    public void RawSerilog_BelowMinLevel()
    {
        _rawSerilog.Debug("This should be filtered out {Value}", 1);
    }

    [Benchmark]
    public void LoggerHelper_BelowMinLevel()
    {
        _loggerHelper.LogDebug("This should be filtered out {Value}", 1);
    }
}
