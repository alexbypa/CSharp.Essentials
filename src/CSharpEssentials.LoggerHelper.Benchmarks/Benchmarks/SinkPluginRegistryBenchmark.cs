using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper;
using Serilog;
using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Micro-benchmark: ConcurrentBag (v5.0.3) vs ConcurrentDictionary&lt;Type,ISinkPlugin&gt; (v5.0.4).
///
/// COSA MISURA
///   Register — idempotency test:
///     Legacy:  ConcurrentBag.Add() aggiunge sempre → il bag cresce senza limiti.
///     Modern:  ConcurrentDictionary.TryAdd() → no-op se il plugin è già presente.
///
///   FindHandler — scansione sotto carico di duplicati:
///     Con RegisterCalls=100, il bag legacy ha 100 entry tutte uguali.
///     Il dizionario moderno ne ha sempre 1 — cerca su 1 elemento a prescindere.
///
/// PERCHÉ È RILEVANTE
///   [ModuleInitializer] viene chiamato per assembly, non per AppDomain.
///   In contesti con più assembly che referenziano lo stesso sink (es. test runner
///   con isolation, web farm con hot reload) il vecchio codice registrava duplicati
///   ogni volta. Con 100 plugin duplicati FindHandler percorreva 100 entry inutili.
///
/// COME ESEGUIRE
///   dotnet run -c Release --framework net9.0 \
///     -- --filter *SinkPluginRegistry* --job short
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SinkPluginRegistryBenchmark {

    // ── Legacy (v5.0.3) — inlined per confronto onesto nella stessa JIT session ──
    private sealed class LegacyRegistry {
        private readonly ConcurrentBag<ISinkPlugin> _plugins = new();

        public void Register(ISinkPlugin plugin) =>
            _plugins.Add(plugin);  // aggiunge sempre, nessun check duplicati

        public ISinkPlugin? FindHandler(string sinkName) =>
            _plugins.FirstOrDefault(p => p.CanHandle(sinkName));
    }

    // ── Modern (v5.0.4) — replica della DefaultSinkPluginRegistry ────────────
    private sealed class ModernRegistry {
        private readonly ConcurrentDictionary<Type, ISinkPlugin> _plugins = new();

        public void Register(ISinkPlugin plugin) =>
            _plugins.TryAdd(plugin.GetType(), plugin);  // no-op se già registrato

        public ISinkPlugin? FindHandler(string sinkName) =>
            _plugins.Values.FirstOrDefault(p => p.CanHandle(sinkName));
    }

    // Stub plugin da registrare
    private sealed class TestSinkPlugin : ISinkPlugin {
        public bool CanHandle(string sinkName) =>
            string.Equals(sinkName, "Test", StringComparison.OrdinalIgnoreCase);
        public void Configure(LoggerConfiguration cfg, SinkRouting routing, LoggerHelperOptions opts) { }
    }

    // Simula quante volte lo stesso plugin viene registrato dallo stesso assembly.
    // Produzione normale = 1. Bug scenario = 10-100 (multi-context loading).
    [Params(1, 10, 100)]
    public int RegisterCalls { get; set; }

    private LegacyRegistry _legacy = null!;
    private ModernRegistry _modern = null!;
    private readonly ISinkPlugin _plugin = new TestSinkPlugin();

    [GlobalSetup]
    public void Setup() {
        _legacy = new LegacyRegistry();
        _modern = new ModernRegistry();

        // Pre-popola entrambi con RegisterCalls registrazioni dello stesso plugin.
        // Bag legacy: crescerà a RegisterCalls entry.
        // Dict moderno: resterà a 1 entry.
        for (var i = 0; i < RegisterCalls; i++) {
            _legacy.Register(_plugin);
            _modern.Register(_plugin);
        }
    }

    // ── Register: costo per aggiunta successiva ───────────────────────────────
    // Misura quanto costa chiamare Register() quando il plugin è già presente.
    // Legacy: paga sempre il costo di Add (thread-contention su ConcurrentBag).
    // Modern: TryAdd fallisce subito con un CAS → overhead minimo.

    [Benchmark(Baseline = true, Description = "Register — Legacy ConcurrentBag.Add (cresce sempre)")]
    public void Register_Legacy() => _legacy.Register(_plugin);

    [Benchmark(Description = "Register — Modern TryAdd (no-op se duplicato)")]
    public void Register_Modern() => _modern.Register(_plugin);

    // ── FindHandler: ricerca per nome sink ────────────────────────────────────
    // Con RegisterCalls=100 il bag legacy ha 100 entry; il dict ha sempre 1.
    // Verifica: la latenza di Legacy_FindHandler scala con RegisterCalls.

    [Benchmark(Description = "FindHandler — Legacy (scansione bag con duplicati)")]
    public ISinkPlugin? FindHandler_Legacy() => _legacy.FindHandler("Test");

    [Benchmark(Description = "FindHandler — Modern (dict.Values, nessun duplicato)")]
    public ISinkPlugin? FindHandler_Modern() => _modern.FindHandler("Test");
}
