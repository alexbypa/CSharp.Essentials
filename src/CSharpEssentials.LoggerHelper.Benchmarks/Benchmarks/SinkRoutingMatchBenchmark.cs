using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Micro-benchmark that isolates SinkRouting.Matches() before and after the v5.0.4 fix.
///
/// COSA MISURA
///   Old: List(string).Contains(level.ToString(), OrdinalIgnoreCase)
///        → un'allocazione stringa per chiamata + scansione O(n)
///   New: HashSet(LogEventLevel).Contains(level)
///        → zero allocazioni + lookup O(1)
///
/// Il parametro LevelCount simula route con 1, 3 o 5 livelli configurati
/// per evidenziare come la differenza O(n) vs O(1) cresca al crescere della lista.
///
/// COME ESEGUIRLO
///   # Solo questo benchmark, run veloce (~1 min):
///   dotnet run -c Release --framework net9.0 -- --filter *SinkRoutingMatch* --job short
///
///   # Confronto completo con tutti i framework:
///   dotnet run -c Release --framework net9.0 -- --filter *SinkRoutingMatch*
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SinkRoutingMatchBenchmark
{
    // ── Legacy implementation (v5.0.3 e precedenti) ──────────────────────────
    // Reinlined here because we removed it from the codebase.
    // Keeps the comparison honest: same test, same JIT, same process.
    private sealed class LegacySinkRouting
    {
        public List<string> Levels { get; set; } = [];

        // Old hot path: alloca una stringa + scansione lineare
        public bool Matches(LogEventLevel level) =>
            Levels.Contains(level.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    // ── Parametro: quanti livelli sono configurati sulla route ────────────────
    [Params(1, 3, 5)]
    public int LevelCount { get; set; }

    private LegacySinkRouting _legacy = null!;
    private SinkRouting _modern = null!;

    // Livelli usati nei test
    private static readonly LogEventLevel _hitLevel  = LogEventLevel.Error;   // presente nella route
    private static readonly LogEventLevel _missLevel = LogEventLevel.Debug;   // assente dalla route

    [GlobalSetup]
    public void Setup()
    {
        // Costruisce una route con LevelCount livelli, includendo sempre Error (hit)
        // ma non Debug (miss) — specchio di una configurazione reale "Warning+ → Email".
        var allLevels = new[]
        {
            "Warning", "Error", "Fatal", "Information", "Verbose"
        };
        var levels = allLevels.Take(LevelCount).ToList();

        _legacy = new LegacySinkRouting { Levels = levels };

        _modern = new SinkRouting { Levels = levels };
        // forza la build del HashSet interno prima del benchmark
        // (warmup naturale di BenchmarkDotNet copre già questo, ma lo esplicitiamo)
        _ = _modern.Matches(_hitLevel);
    }

    // ── HIT: il livello È nella route (caso più frequente in produzione) ──────

    [Benchmark(Baseline = true, Description = "Legacy — hit (List scan)")]
    public bool Legacy_Hit() => _legacy.Matches(_hitLevel);

    [Benchmark(Description = "Modern — hit (HashSet O(1))")]
    public bool Modern_Hit() => _modern.Matches(_hitLevel);

    // ── MISS: il livello NON è nella route (worst case per la lista) ──────────

    [Benchmark(Description = "Legacy — miss (List full scan)")]
    public bool Legacy_Miss() => _legacy.Matches(_missLevel);

    [Benchmark(Description = "Modern — miss (HashSet O(1))")]
    public bool Modern_Miss() => _modern.Matches(_missLevel);
}
