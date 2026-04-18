using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Competitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Confronto delle API di logging:
///   - ILogger.LogInformation (standard MEL, no enrichment)
///   - TraceSync (sincrono con enrichment IdTransaction + Action + scope)
///   - TraceAsync (fire-and-forget con enrichment, offloads su Task.Run)
///   - Serilog raw (baseline)
///   - NLog (competitor)
///
/// Misura l'overhead aggiunto dal pattern TraceSync/TraceAsync rispetto
/// a una semplice chiamata ILogger e ai competitor.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TraceApiBenchmark {
    private Microsoft.Extensions.Logging.ILogger _loggerHelper = null!;
    private ServiceProvider _sp = null!;
    private SerilogCompetitor _serilog = null!;
    private NLogCompetitor _nlog = null!;

    [GlobalSetup]
    public void Setup() {
        // LoggerHelper con single null route
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("TraceBench")
            .DisableOpenTelemetry()
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));
        _sp = services.BuildServiceProvider();
        _loggerHelper = _sp.GetRequiredService<ILoggerProvider>().CreateLogger("TraceBench");

        // Competitor
        _serilog = new SerilogCompetitor();
        _nlog = NLogCompetitor.SingleTarget();
    }

    [GlobalCleanup]
    public void Cleanup() {
        _sp.Dispose();
        _serilog.Dispose();
        _nlog.Dispose();
    }

    // ── Baseline: Serilog raw ──────────────────────────────────────

    [Benchmark(Baseline = true)]
    public void Serilog_Raw()
        => _serilog.Logger.Information("Order {OrderId} total {Amount}", 123, 49.99m);

    // ── NLog ───────────────────────────────────────────────────────

    [Benchmark]
    public void NLog_Raw()
        => _nlog.Logger.Info("Order {OrderId} total {Amount}", 123, 49.99m);

    // ── LoggerHelper: standard ILogger (no enrichment) ─────────────

    [Benchmark]
    public void LoggerHelper_ILogger()
        => _loggerHelper.LogInformation("Order {OrderId} total {Amount}", 123, 49.99m);

    // ── LoggerHelper: TraceSync (with IdTransaction + Action scope) ─

    [Benchmark]
    public void LoggerHelper_TraceSync()
        => _loggerHelper.TraceSync("OrderProcess", "TXN-001",
            LogLevel.Information, null,
            "Order {OrderId} total {Amount}", 123, 49.99m);

    // ── LoggerHelper: TraceAsync (fire-and-forget) ─────────────────

    [Benchmark]
    public void LoggerHelper_TraceAsync()
        => _loggerHelper.TraceAsync("OrderProcess", "TXN-001",
            LogLevel.Information, null,
            "Order {OrderId} total {Amount}", 123, 49.99m);

    // ── LoggerHelper: TraceSync con Exception ──────────────────────

    private static readonly Exception _testException = new InvalidOperationException("test error");

    [Benchmark]
    public void LoggerHelper_TraceSync_WithException()
        => _loggerHelper.TraceSync("OrderProcess", "TXN-ERR",
            LogLevel.Error, _testException,
            "Order {OrderId} failed", 123);

    // ── LoggerHelper: TraceAsync con Exception ─────────────────────

    [Benchmark]
    public void LoggerHelper_TraceAsync_WithException()
        => _loggerHelper.TraceAsync("OrderProcess", "TXN-ERR",
            LogLevel.Error, _testException,
            "Order {OrderId} failed", 123);
}
