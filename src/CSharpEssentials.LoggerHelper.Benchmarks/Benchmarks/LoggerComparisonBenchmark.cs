using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Competitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Head-to-head comparison through the same API: <see cref="Microsoft.Extensions.Logging.ILogger"/>,
/// the way ASP.NET Core apps log. Each framework sits behind its own ILoggerProvider:
/// LoggerHelper (its provider), Serilog (Serilog.Extensions.Logging) and NLog (NLog.Extensions.Logging).
/// Same conditions for all three: same messages and arguments, one no-op sink/target,
/// minimum level Information (Debug is dropped). OpenTelemetry is disabled for LoggerHelper.
/// Difference kept on purpose: LoggerHelper adds its default enrichers (ApplicationName,
/// MachineName, LogContext) and per-level routing; Serilog and NLog run with no enrichers.
/// Small and quick on its own: run it with --anyCategories Quick (or --filter *LoggerComparison*).
/// </summary>
[BenchmarkCategory("Quick")]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class LoggerComparisonBenchmark {
    private SerilogCompetitor _serilog = null!;
    private NLogCompetitor _nlog = null!;
    private SerilogLoggerProvider _serilogProvider = null!;
    private NLogLoggerProvider _nlogProvider = null!;
    private ServiceProvider _sp = null!;

    private Microsoft.Extensions.Logging.ILogger _serilogLogger = null!;
    private Microsoft.Extensions.Logging.ILogger _nlogLogger = null!;
    private Microsoft.Extensions.Logging.ILogger _loggerHelper = null!;

    [GlobalSetup]
    public void Setup() {
        _serilog = new SerilogCompetitor();
        _serilogProvider = new SerilogLoggerProvider(_serilog.Logger, dispose: false);
        _serilogLogger = _serilogProvider.CreateLogger("Benchmark");

        _nlog = NLogCompetitor.SingleTarget();
        _nlogProvider = new NLogLoggerProvider(new NLogProviderOptions(), _nlog.Factory);
        _nlogLogger = _nlogProvider.CreateLogger("Benchmark");

        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("Benchmark")
            .DisableOpenTelemetry()
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));
        _sp = services.BuildServiceProvider();
        _loggerHelper = _sp.GetRequiredService<ILoggerProvider>().CreateLogger("Benchmark");
    }

    [GlobalCleanup]
    public void Cleanup() {
        _serilogProvider.Dispose();
        _nlogProvider.Dispose();
        _serilog.Dispose();
        _nlog.Dispose();
        _sp.Dispose();
    }

    // --- Single message (1 property) ---

    [Benchmark(Baseline = true)]
    public void Serilog_SingleMessage()
        => _serilogLogger.LogInformation("Benchmark message {Counter}", 42);

    [Benchmark]
    public void NLog_SingleMessage()
        => _nlogLogger.LogInformation("Benchmark message {Counter}", 42);

    [Benchmark]
    public void LoggerHelper_SingleMessage()
        => _loggerHelper.LogInformation("Benchmark message {Counter}", 42);

    // --- Structured payload (3 properties) ---

    [Benchmark]
    public void Serilog_StructuredPayload()
        => _serilogLogger.LogInformation("Order {OrderId} for {Customer} total {Amount}", 12345, "Acme Corp", 99.99m);

    [Benchmark]
    public void NLog_StructuredPayload()
        => _nlogLogger.LogInformation("Order {OrderId} for {Customer} total {Amount}", 12345, "Acme Corp", 99.99m);

    [Benchmark]
    public void LoggerHelper_StructuredPayload()
        => _loggerHelper.LogInformation("Order {OrderId} for {Customer} total {Amount}", 12345, "Acme Corp", 99.99m);

    // --- Below minimum level (Debug dropped) ---

    [Benchmark]
    public void Serilog_BelowMinLevel()
        => _serilogLogger.LogDebug("This should be filtered {Value}", 1);

    [Benchmark]
    public void NLog_BelowMinLevel()
        => _nlogLogger.LogDebug("This should be filtered {Value}", 1);

    [Benchmark]
    public void LoggerHelper_BelowMinLevel()
        => _loggerHelper.LogDebug("This should be filtered {Value}", 1);
}
