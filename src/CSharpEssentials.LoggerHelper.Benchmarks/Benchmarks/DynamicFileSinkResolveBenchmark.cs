using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Sinks;
using Serilog.Core;
using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Micro-benchmark: DynamicPropertyFileSink.ResolveSink() before vs after the v5.0.7 fix.
///
/// COSA MISURA
///   Steady state (the overwhelmingly common case): a log event arrives for a
///   FileNameProperty value (e.g. TenantId) that has already been seen, so its
///   per-tenant Serilog file sink already exists in the dictionary.
///
///   Legacy (v5.0.6 and earlier):
///     - ConcurrentDictionary.GetOrAdd(key, factoryLambda) — even on a hit, a new
///       Func&lt;string, SinkEntry&gt; closure capturing `this` is allocated on every call
///       because the C# compiler cannot cache a delegate that captures `this`.
///     - EvictIfNeeded() runs unconditionally and calls ConcurrentDictionary.Count,
///       which acquires every internal table lock to compute an exact count.
///
///   Modern (v5.0.7):
///     - ConcurrentDictionary.TryGetValue(key, out entry) — no delegate allocation.
///     - EvictIfNeeded()/Count is only reached on the cold path (a brand-new key),
///       gated by a cheap Interlocked counter.
///
/// Il parametro TenantCount simula quanti tenant/sink dinamici sono attualmente
/// aperti — più cresce, più costoso diventa ConcurrentDictionary.Count nel path legacy.
///
/// COME ESEGUIRLO
///   dotnet run -c Release --framework net9.0 -- --filter *DynamicFileSinkResolve* --job short --exporters github
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DynamicFileSinkResolveBenchmark {

    private sealed class SinkEntry {
        public ILogEventSink Sink { get; }
        public DateTime LastUsed { get; private set; }
        public SinkEntry(ILogEventSink sink) { Sink = sink; LastUsed = DateTime.UtcNow; }
        public void Touch() => LastUsed = DateTime.UtcNow;
    }

    // ── Legacy (v5.0.6) — reinlined for an honest side-by-side comparison ────────
    private sealed class LegacyResolver {
        private readonly ConcurrentDictionary<string, SinkEntry> _sinks = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _evictionLock = new();
        private const int MaxOpenFiles = 64;

        public void Seed(string key) =>
            _sinks.GetOrAdd(key, k => new SinkEntry(new NullSink()));

        // Mirrors DynamicPropertyFileSink.ResolveSink() pre-v5.0.7
        public ILogEventSink Resolve(string key) {
            var entry = _sinks.GetOrAdd(key, k => new SinkEntry(new NullSink()));
            entry.Touch();
            EvictIfNeeded();
            return entry.Sink;
        }

        private void EvictIfNeeded() {
            if (_sinks.Count <= MaxOpenFiles)
                return;

            lock (_evictionLock) {
                if (_sinks.Count <= MaxOpenFiles)
                    return;

                var toEvict = _sinks
                    .OrderBy(kv => kv.Value.LastUsed)
                    .Take(_sinks.Count - MaxOpenFiles)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var k in toEvict)
                    _sinks.TryRemove(k, out _);
            }
        }
    }

    // ── Modern (v5.0.7) — TryGetValue fast path + Interlocked-gated eviction ─────
    private sealed class ModernResolver {
        private readonly ConcurrentDictionary<string, SinkEntry> _sinks = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _evictionLock = new();
        private int _sinkCount;
        private const int MaxOpenFiles = 64;

        public void Seed(string key) {
            if (_sinks.TryAdd(key, new SinkEntry(new NullSink())))
                Interlocked.Increment(ref _sinkCount);
        }

        // Mirrors DynamicPropertyFileSink.ResolveSink() from v5.0.7
        public ILogEventSink Resolve(string key) {
            if (_sinks.TryGetValue(key, out var cached)) {
                cached.Touch();
                return cached.Sink;
            }
            return ResolveNew(key);
        }

        private ILogEventSink ResolveNew(string key) {
            var candidate = new SinkEntry(new NullSink());
            var entry = _sinks.GetOrAdd(key, candidate);

            if (ReferenceEquals(entry, candidate)) {
                if (Interlocked.Increment(ref _sinkCount) > MaxOpenFiles)
                    EvictIfNeeded();
            }

            entry.Touch();
            return entry.Sink;
        }

        private void EvictIfNeeded() {
            lock (_evictionLock) {
                var excess = _sinks.Count - MaxOpenFiles;
                if (excess <= 0)
                    return;

                var toEvict = _sinks
                    .OrderBy(kv => kv.Value.LastUsed)
                    .Take(excess)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var k in toEvict) {
                    if (_sinks.TryRemove(k, out _))
                        Interlocked.Decrement(ref _sinkCount);
                }
            }
        }
    }

    // Number of distinct tenants already tracked when the benchmarked call runs.
    [Params(8, 64, 256)]
    public int TenantCount { get; set; }

    private LegacyResolver _legacy = null!;
    private ModernResolver _modern = null!;
    private const string HitKey = "tenant-0042";

    [GlobalSetup]
    public void Setup() {
        _legacy = new LegacyResolver();
        _modern = new ModernResolver();

        for (var i = 0; i < TenantCount; i++) {
            var key = $"tenant-{i:D4}";
            _legacy.Seed(key);
            _modern.Seed(key);
        }
        _legacy.Seed(HitKey);
        _modern.Seed(HitKey);
    }

    [Benchmark(Baseline = true, Description = "Legacy — GetOrAdd(factory) + Count-based evict")]
    public ILogEventSink Legacy_Resolve_ExistingTenant() => _legacy.Resolve(HitKey);

    [Benchmark(Description = "Modern — TryGetValue + Interlocked-gated evict")]
    public ILogEventSink Modern_Resolve_ExistingTenant() => _modern.Resolve(HitKey);
}