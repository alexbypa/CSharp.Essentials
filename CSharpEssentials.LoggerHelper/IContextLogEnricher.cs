using Serilog;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Interface for enriching Serilog loggers with additional context information.
/// </summary>
public interface IContextLogEnricher {
    /// <summary>
    /// Enriches the given <see cref="ILogger"/> instance with custom context data.
    /// </summary>
    /// <param name="logger">The logger instance to enrich.</param>
    /// <param name="context">Optional context data to add to the logger.</param>
    /// <returns>The enriched <see cref="ILogger"/> instance.</returns>
    ILogger Enrich(ILogger logger, object? context);
    /// <summary>
    /// Enriches the given <see cref="LoggerConfiguration"/> instance with additional configuration or enrichers.
    /// </summary>
    /// <param name="configuration">The logger configuration to enrich.</param>
    /// <returns>The enriched <see cref="LoggerConfiguration"/> instance.</returns>
    LoggerConfiguration Enrich(LoggerConfiguration configuration);

}
