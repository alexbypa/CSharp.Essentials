using Serilog;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Optional enricher for global pipeline configuration and per-log Serilog context.
/// Register with: services.AddSingleton&lt;IContextLogEnricher, MyEnricher&gt;();
/// </summary>
public interface IContextLogEnricher {
    /// <summary>
    /// Enriches the Serilog logger for a single write (e.g. with request context).
    /// </summary>
    ILogger Enrich(ILogger logger, object? context);

    /// <summary>
    /// Enriches the Serilog pipeline at startup.
    /// </summary>
    LoggerConfiguration Enrich(LoggerConfiguration configuration);
}
