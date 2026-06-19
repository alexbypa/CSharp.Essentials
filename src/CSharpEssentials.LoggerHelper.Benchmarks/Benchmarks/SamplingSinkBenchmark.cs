using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Measures the overhead of ShouldEmit() vs Matches() to prove sampling
/// adds negligible cost when the rate is null (disabled).
///
/// Three scenarios:
///   1. Matches()       — level check only (baseline)
///   2. ShouldEmit(null) — sampling disabled (should be ~identical to Matches)
///   3. ShouldEmit(0.5) — sampling enabled (adds Random.Shared.NextDouble())
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SamplingSinkBenchmark {
    private SinkRouting _noSampling = null!;
    private SinkRouting _halfSampling = null!;

    private static readonly LogEventLevel _hitLevel = LogEventLevel.Information;

    [GlobalSetup]
    public void Setup() {
        _noSampling = new SinkRouting {
            Sink = "Console",
            Levels = ["Information", "Warning", "Error", "Fatal"],
            SamplingRate = null
        };
        _ = _noSampling.Matches(_hitLevel);

        _halfSampling = new SinkRouting {
            Sink = "Elasticsearch",
            Levels = ["Information", "Warning", "Error", "Fatal"],
            SamplingRate = 0.5
        };
        _ = _halfSampling.Matches(_hitLevel);
    }

    [Benchmark(Baseline = true, Description = "Matches() — level check only")]
    public bool Matches_Only() => _noSampling.Matches(_hitLevel);

    [Benchmark(Description = "ShouldEmit(null) — sampling disabled")]
    public bool ShouldEmit_NoSampling() => _noSampling.ShouldEmit(_hitLevel);

    [Benchmark(Description = "ShouldEmit(0.5) — 50% sampling")]
    public bool ShouldEmit_HalfRate() => _halfSampling.ShouldEmit(_hitLevel);
}
