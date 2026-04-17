using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Competitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Confronto throughput: LoggerHelper v5 vs Serilog raw (baseline) vs NLog.
/// Tutti usano sink no-op — misura overhead del framework, non I/O.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ThroughputBenchmark
{
    private SerilogCompetitor _serilog = null!;
    private NLogCompetitor _nlog = null!;
    private Microsoft.Extensions.Logging.ILogger _loggerHelper = null!;
    private ServiceProvider _sp = null!;

    [GlobalSetup]
    public void Setup()
    {
        _serilog = new SerilogCompetitor();
        _nlog = NLogCompetitor.SingleTarget();

        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("Benchmark")
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));
        _sp = services.BuildServiceProvider();
        _loggerHelper = _sp.GetRequiredService<ILoggerProvider>().CreateLogger("Benchmark");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serilog.Dispose();
        _nlog.Dispose();
        _sp.Dispose();
    }

    // --- Single message ---

    [Benchmark(Baseline = true)]
    public void Serilog_SingleMessage()
        => _serilog.Logger.Information("Benchmark message {Counter}", 42);

    [Benchmark]
    public void NLog_SingleMessage()
        => _nlog.Logger.Info("Benchmark message {Counter}", 42);

    [Benchmark]
    public void LoggerHelper_SingleMessage()
        => _loggerHelper.LogInformation("Benchmark message {Counter}", 42);

    // --- Structured payload (3 properties) ---

    [Benchmark]
    public void Serilog_StructuredPayload()
        => _serilog.Logger.Information(
            "Order {OrderId} for {Customer} total {Amount}",
            12345, "Acme Corp", 99.99m);

    [Benchmark]
    public void NLog_StructuredPayload()
        => _nlog.Logger.Info(
            "Order {OrderId} for {Customer} total {Amount}",
            12345, "Acme Corp", 99.99m);

    [Benchmark]
    public void LoggerHelper_StructuredPayload()
        => _loggerHelper.LogInformation(
            "Order {OrderId} for {Customer} total {Amount}",
            12345, "Acme Corp", 99.99m);

    // --- Below MinLevel (Debug filtered out in prod) ---

    [Benchmark]
    public void Serilog_BelowMinLevel()
        => _serilog.Logger.Debug("This should be filtered {Value}", 1);

    [Benchmark]
    public void NLog_BelowMinLevel()
        => _nlog.Logger.Debug("This should be filtered {Value}", 1);

    [Benchmark]
    public void LoggerHelper_BelowMinLevel()
        => _loggerHelper.LogDebug("This should be filtered {Value}", 1);
}
