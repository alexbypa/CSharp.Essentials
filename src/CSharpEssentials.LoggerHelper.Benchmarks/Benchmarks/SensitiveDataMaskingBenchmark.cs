using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Costo per-evento dell'enricher di sensitive data masking (v5.0.8 — nuova feature).
///
/// Tre scenari, tutti su sink Null (overhead di framework, non I/O):
///   1. Baseline    — masking disabilitato (default, costo zero per chi non lo usa)
///   2. 1 preset    — solo "Email" attivo, messaggio senza match (fast path: nessuna sostituzione)
///   3. 5 presets   — tutti i preset built-in attivi + 1 regex custom + 1 SensitiveProperties,
///                    con un messaggio che contiene email + carta di credito (match reale)
///
/// COME ESEGUIRE
///   dotnet run -c Release --framework net9.0 \
///     -- --filter *SensitiveDataMasking* --job short
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SensitiveDataMaskingBenchmark {
    private ServiceProvider _spBaseline = null!;
    private ServiceProvider _spOnePreset = null!;
    private ServiceProvider _spFiveRules = null!;

    private ILogger _baseline = null!;
    private ILogger _onePreset = null!;
    private ILogger _fiveRules = null!;

    [GlobalSetup]
    public void Setup() {
        _spBaseline = BuildProvider(b => { });
        _baseline = _spBaseline.GetRequiredService<ILoggerProvider>().CreateLogger("Benchmark");

        _spOnePreset = BuildProvider(b => b.EnableSensitiveDataMasking(o => {
            o.Presets.Add("Email");
        }));
        _onePreset = _spOnePreset.GetRequiredService<ILoggerProvider>().CreateLogger("Benchmark");

        _spFiveRules = BuildProvider(b => b.EnableSensitiveDataMasking(o => {
            o.Presets.AddRange(["Email", "CreditCard", "JwtToken", "BearerToken", "ConnectionStringSecret"]);
            o.SensitiveProperties.Add("Password");
            o.Rules.Add(new MaskingRule { Name = "OrderId", Pattern = @"ORD-\d+" });
        }));
        _fiveRules = _spFiveRules.GetRequiredService<ILoggerProvider>().CreateLogger("Benchmark");
    }

    [GlobalCleanup]
    public void Cleanup() {
        _spBaseline.Dispose();
        _spOnePreset.Dispose();
        _spFiveRules.Dispose();
    }

    private static ServiceProvider BuildProvider(Action<LoggerHelperBuilder> configureMasking) {
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => {
            b.WithApplicationName("Benchmark")
             .DisableOpenTelemetry()
             .AddRoute("Null",
                 LogEventLevel.Information,
                 LogEventLevel.Warning,
                 LogEventLevel.Error,
                 LogEventLevel.Fatal);
            configureMasking(b);
        });
        return services.BuildServiceProvider();
    }

    // --- No match in payload: worst case for "is masking always-on costly?" ---

    [Benchmark(Baseline = true, Description = "Masking disabled — structured payload, no PII")]
    public void Baseline_NoMasking()
        => _baseline.LogInformation("Order {OrderId} for {Customer} total {Amount}", 12345, "Acme Corp", 99.99m);

    [Benchmark(Description = "1 preset (Email) enabled — payload contains no email (no match)")]
    public void OnePreset_NoMatch()
        => _onePreset.LogInformation("Order {OrderId} for {Customer} total {Amount}", 12345, "Acme Corp", 99.99m);

    // --- Real-world PII payload: 5 presets + custom rule + sensitive property, with matches ---

    [Benchmark(Description = "5 presets + custom rule + SensitiveProperties — payload contains email, card, order id, password")]
    public void FiveRules_WithMatches()
        => _fiveRules.LogInformation(
            "Checkout for {Email}, card {CardNumber}, {OrderId}, auth {Password}",
            "alice@example.com", "4532-1234-5678-9012", "ORD-99821", "Sup3rSecret!");
}
