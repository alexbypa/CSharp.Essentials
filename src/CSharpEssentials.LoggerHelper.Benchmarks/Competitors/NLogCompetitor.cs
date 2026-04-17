using NLog;
using NLog.Config;
using NLog.Targets;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Competitors;

/// <summary>
/// Setup isolato per NLog — usa LogFactory (non il singleton globale)
/// così più configurazioni possono coesistere nello stesso processo.
/// </summary>
internal sealed class NLogCompetitor : IDisposable
{
    private readonly LogFactory _factory;

    public NLogCompetitor(Action<LoggingConfiguration> configure)
    {
        var config = new LoggingConfiguration();
        configure(config);
        _factory = new LogFactory { Configuration = config };
    }

    public Logger Logger => _factory.GetLogger("Benchmark");

    public void Dispose() => _factory.Shutdown();

    // --- Factory methods per i casi d'uso comuni ---

    /// <summary>Single NullTarget — minimo overhead, usato come baseline NLog.</summary>
    public static NLogCompetitor SingleTarget() =>
        new(cfg =>
        {
            var t = new NullTarget("null");
            cfg.AddRule(LogLevel.Info, LogLevel.Fatal, t);
        });

    /// <summary>
    /// Due NullTarget con regole separate: primo per Info-Error, secondo per Error-Fatal.
    /// Simula routing multi-sink per confronto con LoggerHelper.
    /// </summary>
    public static NLogCompetitor MultiTarget() =>
        new(cfg =>
        {
            var t1 = new NullTarget("null-info-error");
            var t2 = new NullTarget("null-error-fatal");
            cfg.AddRule(LogLevel.Info, LogLevel.Error, t1);
            cfg.AddRule(LogLevel.Error, LogLevel.Fatal, t2);
        });
}
