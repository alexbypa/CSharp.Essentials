using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Does sensitive data masking get more expensive with more sinks?
/// Masking runs as an enricher on the root pipeline (LoggerPipelineFactory), once per event,
/// before the event is routed to the sinks. So the masking cost (Masking_On minus Masking_Off)
/// should stay flat at 1, 3 and 5 sinks, while both rows grow with the sink fan-out.
/// Payload contains an email, a card number and a password; all no-op sinks, OpenTelemetry off.
/// </summary>
[BenchmarkCategory("Quick")]
[MemoryDiagnoser]
[RankColumn]
public class MaskingSinkScalingBenchmark {
    private ServiceProvider _spOff = null!;
    private ServiceProvider _spOn = null!;
    private ILogger _off = null!;
    private ILogger _on = null!;

    [Params(1, 3, 5)]
    public int Sinks { get; set; }

    [GlobalSetup]
    public void Setup() {
        _spOff = BuildProvider(Sinks, masking: false);
        _off = _spOff.GetRequiredService<ILoggerProvider>().CreateLogger("Benchmark");

        _spOn = BuildProvider(Sinks, masking: true);
        _on = _spOn.GetRequiredService<ILoggerProvider>().CreateLogger("Benchmark");
    }

    [GlobalCleanup]
    public void Cleanup() {
        _spOff.Dispose();
        _spOn.Dispose();
    }

    private static ServiceProvider BuildProvider(int sinks, bool masking) {
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => {
            b.WithApplicationName("Benchmark").DisableOpenTelemetry();
            // Null1..NullN: each route is a separate no-op sink (NullSinkPlugin handles "Null*").
            for (int i = 1; i <= sinks; i++)
                b.AddRoute($"Null{i}",
                    LogEventLevel.Information,
                    LogEventLevel.Warning,
                    LogEventLevel.Error,
                    LogEventLevel.Fatal);
            if (masking)
                b.EnableSensitiveDataMasking(o => {
                    o.Presets.AddRange(["Email", "CreditCard", "JwtToken", "BearerToken", "ConnectionStringSecret"]);
                    o.SensitiveProperties.Add("Password");
                });
        });
        return services.BuildServiceProvider();
    }

    [Benchmark(Baseline = true)]
    public void Masking_Off()
        => _off.LogInformation("Checkout for {Email}, card {CardNumber}, auth {Password}",
            "alice@example.com", "4532-1234-5678-9012", "Sup3rSecret!");

    [Benchmark]
    public void Masking_On()
        => _on.LogInformation("Checkout for {Email}, card {CardNumber}, auth {Password}",
            "alice@example.com", "4532-1234-5678-9012", "Sup3rSecret!");
}
