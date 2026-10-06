using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Baselines;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Contention benchmark: current locked <see cref="ContextualLogBuffer"/> vs the pre-A54 lock-free
/// copy (baseline). A fixed total of pushes is split across N concurrent threads, so Mean is the
/// wall-clock per push (inverse of aggregate throughput, not single-call latency). In the
/// *PushWithFlush variants every partition calls FlushAndClear every FlushEvery of its own pushes.
/// Note: the lock-free baseline has known races (it is NOT thread-safe), so its correctness
/// is not verified here — only its speed is used as a reference.
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ContextualLogBufferContentionBenchmark
{
    private const int TotalPushes = 1_048_576;

    private LockFreeContextualLogBuffer _lockFree = null!;
    private ContextualLogBuffer _locked = null!;
    private const string Message = "benchmark message";
    private DateTime _timestamp;
    private LogBufferEntry _triggeringError = null!;
    private LockFreeLogBufferEntry _lockFreeTriggeringError = null!;

    [Params(1, 4, 16, 64)]
    public int Threads { get; set; }

    // Every partition flushes once per FlushEvery of its own pushes: 10 = error burst, 1000 = occasional.
    // Only the *PushWithFlush methods use it; the pure Push methods run once per value, so their
    // rows are duplicates (accepted: BenchmarkDotNet has no per-method Params).
    [Params(10, 1000)]
    public int FlushEvery { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Process-wide: SetMinThreads is global and stays in effect after this benchmark.
        ThreadPool.SetMinThreads(64, 64);
        _lockFree = new LockFreeContextualLogBuffer(100);
        _locked = new ContextualLogBuffer(100);
        _timestamp = DateTime.UtcNow;
        _triggeringError = new LogBufferEntry { Level = LogEventLevel.Error, Message = Message, Timestamp = _timestamp };
        _lockFreeTriggeringError = new LockFreeLogBufferEntry { Level = LogEventLevel.Error, Message = Message, Timestamp = _timestamp };
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = TotalPushes)]
    public void LockFree_Push() =>
        Run(() => _lockFree.Push(LogEventLevel.Information, Message, null, _timestamp), null);

    [Benchmark(OperationsPerInvoke = TotalPushes)]
    public void Locked_Push() =>
        Run(() => _locked.Push(LogEventLevel.Information, Message, null, _timestamp), null);

    [Benchmark(OperationsPerInvoke = TotalPushes)]
    public void LockFree_PushWithFlush() =>
        Run(() => _lockFree.Push(LogEventLevel.Information, Message, null, _timestamp),
            () => _lockFree.FlushAndClear(_lockFreeTriggeringError));

    [Benchmark(OperationsPerInvoke = TotalPushes)]
    public void Locked_PushWithFlush() =>
        Run(() => _locked.Push(LogEventLevel.Information, Message, null, _timestamp),
            () => _locked.FlushAndClear(_triggeringError));

    private void Run(Action push, Action? flush)
    {
        int perThread = TotalPushes / Threads;
        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, _ =>
        {
            for (int i = 1; i <= perThread; i++)
            {
                push();
                if (flush is not null && i % FlushEvery == 0)
                    flush();
            }
        });
    }
}
