using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Competitors;
using CSharpEssentials.LoggerHelper.Benchmarks.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Routing overhead: single-sink vs multi-sink per LoggerHelper, Serilog, NLog.
/// Differenziatore chiave di LoggerHelper: routing dichiarativo per livello.
/// OpenTelemetry è disabilitato per un confronto equo.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RoutingBenchmark
{
    // LoggerHelper
    private Microsoft.Extensions.Logging.ILogger _lhSingle = null!;
    private Microsoft.Extensions.Logging.ILogger _lhMulti = null!;
    private ServiceProvider _sp1 = null!;
    private ServiceProvider _sp2 = null!;

    // Serilog
    private Serilog.ILogger _serilogSingle = null!;
    private Serilog.ILogger _serilogMulti = null!;
    private Logger _serilogSingleRoot = null!;
    private Logger _serilogMultiRoot = null!;

    // NLog
    private NLogCompetitor _nlogSingle = null!;
    private NLogCompetitor _nlogMulti = null!;

    [GlobalSetup]
    public void Setup()
    {
        // LoggerHelper — single route
        var s1 = new ServiceCollection();
        s1.AddLoggerHelper(b => b
            .WithApplicationName("SingleRoute")
            .DisableOpenTelemetry()
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));
        _sp1 = s1.BuildServiceProvider();
        _lhSingle = _sp1.GetRequiredService<ILoggerProvider>().CreateLogger("Single");

        // LoggerHelper — multi route (NullA: Info/Warning/Error, NullB: Error/Fatal)
        var s2 = new ServiceCollection();
        s2.AddLoggerHelper(b => b
            .WithApplicationName("MultiRoute")
            .DisableOpenTelemetry()
            .AddRoute("NullA", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error)
            .AddRoute("NullB", LogEventLevel.Error, LogEventLevel.Fatal));
        _sp2 = s2.BuildServiceProvider();
        _lhMulti = _sp2.GetRequiredService<ILoggerProvider>().CreateLogger("Multi");

        // Serilog — single sink
        _serilogSingleRoot = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(new NullSink())
            .CreateLogger();
        _serilogSingle = _serilogSingleRoot;

        // Serilog — multi sub-logger: Info/Warning/Error → sink1, Error/Fatal → sink2.
        // Error hits both sub-loggers (intentional overlap) — mirrors LoggerHelper NullA+NullB behavior.
        _serilogMultiRoot = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e =>
                    e.Level >= LogEventLevel.Information &&
                    e.Level <= LogEventLevel.Error)
                .WriteTo.Sink(new NullSink()))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                .WriteTo.Sink(new NullSink()))
            .CreateLogger();
        _serilogMulti = _serilogMultiRoot;

        // NLog
        _nlogSingle = NLogCompetitor.SingleTarget();
        _nlogMulti = NLogCompetitor.MultiTarget();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _sp1.Dispose();
        _sp2.Dispose();
        _serilogSingleRoot.Dispose();
        _serilogMultiRoot.Dispose();
        _nlogSingle.Dispose();
        _nlogMulti.Dispose();
    }

    // --- Single sink/target/route baseline ---

    [Benchmark(Baseline = true)]
    public void Serilog_Single_Info()
        => _serilogSingle.Information("Routing test {Value}", 1);

    [Benchmark]
    public void NLog_Single_Info()
        => _nlogSingle.Logger.Info("Routing test {Value}", 1);

    [Benchmark]
    public void LoggerHelper_Single_Info()
        => _lhSingle.LogInformation("Routing test {Value}", 1);

    // --- Multi sink: messaggio Info → 1 destinazione ---

    [Benchmark]
    public void Serilog_Multi_Info()
        => _serilogMulti.Information("Routing test {Value}", 1);

    [Benchmark]
    public void NLog_Multi_Info()
        => _nlogMulti.Logger.Info("Routing test {Value}", 1);

    [Benchmark]
    public void LoggerHelper_Multi_Info()
        => _lhMulti.LogInformation("Routing test {Value}", 1);

    // --- Multi sink: messaggio Error → 2 destinazioni ---

    [Benchmark]
    public void Serilog_Multi_Error()
        => _serilogMulti.Error("Routing test {Value}", 1);

    [Benchmark]
    public void NLog_Multi_Error()
        => _nlogMulti.Logger.Error("Routing test {Value}", 1);

    [Benchmark]
    public void LoggerHelper_Multi_Error()
        => _lhMulti.LogError("Routing test {Value}", 1);
}
