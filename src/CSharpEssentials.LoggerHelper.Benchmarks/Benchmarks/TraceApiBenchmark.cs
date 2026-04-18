using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Competitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Confronto delle API di logging:
///   - Serilog raw (baseline)
///   - NLog (competitor)
///   - ILogger standard (LogInformation/LogError)
///   - Trace single-shot (template appending, no scope)
///   - BeginTrace scope (set once, standard ILogger inside — amortizzato su N log)
///
/// Tutti usano sink no-op — misura overhead del framework, non I/O.
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

        _serilog = new SerilogCompetitor();
        _nlog = NLogCompetitor.SingleTarget();
    }

    [GlobalCleanup]
    public void Cleanup() {
        _sp.Dispose();
        _serilog.Dispose();
        _nlog.Dispose();
    }

    // ── Baseline ───────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public void Serilog_Raw()
        => _serilog.Logger.Information("Order {OrderId} total {Amount}", 123, 49.99m);

    [Benchmark]
    public void NLog_Raw()
        => _nlog.Logger.Info("Order {OrderId} total {Amount}", 123, 49.99m);

    // ── ILogger standard (no enrichment) ───────────────────────────

    [Benchmark]
    public void LoggerHelper_ILogger()
        => _loggerHelper.LogInformation("Order {OrderId} total {Amount}", 123, 49.99m);

    // ── ILogger with exception ─────────────────────────────────────

    private static readonly Exception _testEx = new InvalidOperationException("test");

    [Benchmark]
    public void LoggerHelper_ILogger_WithException()
        => _loggerHelper.LogError(_testEx, "Order {OrderId} failed", 123);

    // ── Trace single-shot (inline enrichment, no scope) ────────────

    [Benchmark]
    public void LoggerHelper_Trace()
        => _loggerHelper.Trace("OrderProcess", "TXN-001",
            LogLevel.Information, null,
            "Order {OrderId} total {Amount}", 123, 49.99m);

    // ── Trace with Exception ───────────────────────────────────────

    [Benchmark]
    public void LoggerHelper_Trace_WithException()
        => _loggerHelper.Trace("OrderProcess", "TXN-ERR",
            LogLevel.Error, _testEx,
            "Order {OrderId} failed", 123);

    // ── BeginTrace scope + ILogger (amortized cost) ────────────────
    // Simulates real-world: scope set once, 5 logs inside.
    // Cost per log = (scope cost + 5 × log cost) / 5

    [Benchmark(OperationsPerInvoke = 5)]
    public void LoggerHelper_BeginTrace_5Logs() {
        using (_loggerHelper.BeginTrace("OrderProcess", "TXN-001")) {
            _loggerHelper.LogInformation("Step 1: validate {OrderId}", 123);
            _loggerHelper.LogInformation("Step 2: reserve stock {ProductId}", 456);
            _loggerHelper.LogWarning("Step 3: low inventory {Qty}", 2);
            _loggerHelper.LogInformation("Step 4: charge {Amount}", 49.99m);
            _loggerHelper.LogInformation("Step 5: confirm {OrderId}", 123);
        }
    }
}
