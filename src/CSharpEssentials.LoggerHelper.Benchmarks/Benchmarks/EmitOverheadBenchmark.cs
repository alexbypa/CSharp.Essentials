using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Diagnostics;
using System.Collections.Concurrent;
using System.Net;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Misura l'overhead di Emit() per i tre fix comportamentali di v5.0.4:
///
///   1. TELEGRAM — blocking GetResult vs fire-and-forget Task.Run
///      Emit() è chiamato sincrono da Serilog. Legacy: blocca il thread flush
///      per tutta la durata della chiamata HTTP. Modern: torna in ~2µs.
///      Benchmark: misura il meccanismo di dispatch, non la rete.
///
///   2. EMAIL — File.ReadAllText + string rebuild vs template cached al costruttore
///      Legacy: File.ReadAllText() + LoadDefaultTemplate() su ogni Emit().
///      Modern: _template già in memoria, paga solo il costo di string.Replace.
///
///   3. THROTTLING — check-then-set non-atomico vs CAS TryUpdate loop
///      Legacy: race condition TOCTOU (_lastSent[key] = now non è atomico).
///      Modern: TryUpdate CAS garantisce un solo thread vince per slot.
///      Benchmark: conferma che il CAS non introduce regressione apprezzabile.
///
/// COME ESEGUIRE
///   dotnet run -c Release --framework net9.0 \
///     -- --filter *EmitOverhead* --job short
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class EmitOverheadBenchmark {

    // ═══════════════════════════════════════════════════════════════════════════
    // SEZIONE 1 — TELEGRAM: dispatch overhead
    // ═══════════════════════════════════════════════════════════════════════════

    // Task già completato — isola il meccanismo di blocking senza I/O di rete.
    // In produzione il task è la chiamata HTTP: latency reale 100ms–2s.
    private static readonly Task _completedTask = Task.CompletedTask;

    [Benchmark(Baseline = true, Description = "Telegram legacy — GetAwaiter().GetResult() blocca il chiamante")]
    public void Telegram_Legacy_Block() =>
        _completedTask.GetAwaiter().GetResult();

    [Benchmark(Description = "Telegram modern — Task.Run fire-and-forget, Emit() torna in ~2µs")]
    public void Telegram_Modern_Dispatch() =>
        _ = Task.Run(static () => { /* lavoro reale gira off-thread */ });

    // ═══════════════════════════════════════════════════════════════════════════
    // SEZIONE 2 — EMAIL: template caching
    // ═══════════════════════════════════════════════════════════════════════════

    private static readonly string _fakeTimestamp  = "2026-06-06 12:00:00";
    private static readonly string _fakeLevel      = "Error";
    private static readonly string _fakeLevelClass = "level-error";
    private static readonly string _fakeMessage    = "Payment gateway timeout for Order 42";

    private string _cachedTemplate = null!;
    private string _tempFilePath   = null!;

    [GlobalSetup]
    public void Setup() {
        // Email: prepara template cached e file temporaneo
        _cachedTemplate = BuildHtmlTemplate();
        _tempFilePath = Path.GetTempFileName();
        File.WriteAllText(_tempFilePath, BuildHtmlTemplate());

        // Throttling: pre-popola le entry nel dizionario per evitare il costo
        // del primo GetOrAdd nel corpo dei benchmark
        _legacyThrottle.CanSend("Telegram", _zeroInterval);
        SinkThrottlingManager.CanSend("Telegram", _zeroInterval);
    }

    [GlobalCleanup]
    public void Cleanup() {
        if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
    }

    [Benchmark(Description = "Email legacy — LoadDefaultTemplate() + Replace ad ogni Emit (no file)")]
    public string Email_Legacy_RebuildPerCall() {
        // Simula vecchio codice senza TemplatePath configurato:
        //   var template = LoadDefaultTemplate();
        //   return GenerateHtmlBody(logEvent, template);
        var template = BuildHtmlTemplate();
        return ApplyReplacements(template);
    }

    [Benchmark(Description = "Email legacy — File.ReadAllText() + Replace ad ogni Emit (produzione)")]
    public string Email_Legacy_FileReadPerCall() {
        // Simula vecchio codice con TemplatePath: I/O disco sul hot path
        //   var template = File.ReadAllText(_opts.TemplatePath);
        //   return GenerateHtmlBody(logEvent, template);
        var template = File.ReadAllText(_tempFilePath);
        return ApplyReplacements(template);
    }

    [Benchmark(Description = "Email modern — template cached al costruttore, solo Replace")]
    public string Email_Modern_Cached() {
        // Nuovo codice: _template in memoria dal costruttore.
        // Il costo fisso di Replace è inevitabile; tutto il resto è eliminato.
        return ApplyReplacements(_cachedTemplate);
    }

    private static string BuildHtmlTemplate() =>
        "<html><head><style>" +
        "body{font-family:Arial,sans-serif} .level-error{color:red;font-weight:bold}" +
        "</style></head><body>" +
        "<div>Timestamp: {{Timestamp}}</div>" +
        "<div class='{{LevelClass}}'>Level: {{Level}}</div>" +
        "<pre>{{Message}}</pre>" +
        "</body></html>";

    private string ApplyReplacements(string template) =>
        template
            .Replace("{{Timestamp}}", _fakeTimestamp)
            .Replace("{{Level}}", _fakeLevel)
            .Replace("{{LevelClass}}", _fakeLevelClass)
            .Replace("{{Message}}", WebUtility.HtmlEncode(_fakeMessage));

    // ═══════════════════════════════════════════════════════════════════════════
    // SEZIONE 3 — THROTTLING: check-then-set vs CAS loop
    // ═══════════════════════════════════════════════════════════════════════════

    // Legacy inlined — stessa logica del vecchio SinkThrottlingManager
    private sealed class LegacyThrottling {
        private readonly ConcurrentDictionary<string, DateTime> _lastSent = new();

        public bool CanSend(string sinkName, TimeSpan interval) {
            if (interval <= TimeSpan.Zero) return true;
            var now = DateTime.UtcNow;
            var last = _lastSent.GetOrAdd(sinkName, DateTime.MinValue);
            if (now - last < interval) return false;
            _lastSent[sinkName] = now;  // ← non-atomic: race condition v5.0.3
            return true;
        }
    }

    private readonly LegacyThrottling _legacyThrottle = new();

    // Fast path: interval=0 → ramo ottimizzato, ritorna true immediatamente
    private static readonly TimeSpan _zeroInterval = TimeSpan.Zero;

    // Throttled path: intervallo molto grande → il log è sicuramente throttled
    private static readonly TimeSpan _bigInterval = TimeSpan.FromDays(1);

    [Benchmark(Description = "Throttle legacy — fast path (interval=0, ritorna true)")]
    public bool Throttle_Legacy_FastPath() =>
        _legacyThrottle.CanSend("Telegram", _zeroInterval);

    [Benchmark(Description = "Throttle modern — fast path (CAS, interval=0)")]
    public bool Throttle_Modern_FastPath() =>
        SinkThrottlingManager.CanSend("Telegram", _zeroInterval);

    [Benchmark(Description = "Throttle legacy — throttled path (GetOrAdd + check, ritorna false)")]
    public bool Throttle_Legacy_Throttled() =>
        _legacyThrottle.CanSend("Telegram", _bigInterval);

    [Benchmark(Description = "Throttle modern — throttled path (GetOrAdd + CAS, ritorna false)")]
    public bool Throttle_Modern_Throttled() =>
        SinkThrottlingManager.CanSend("Telegram", _bigInterval);
}
