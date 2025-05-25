using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Extension methods for the SerilogConfiguration class, providing additional
/// logic to validate or inspect Serilog sink configurations.
/// </summary>
public static class SerilogConfigExtensions {
    /// <summary>
    /// Determines if the specified log level is configured for the given sink.
    /// This method checks if the provided sink has the log level included in its configuration.
    /// </summary>
    /// <param name="config">The SerilogConfiguration instance containing all sink configurations.</param>
    /// <param name="sink">The name of the sink to check (e.g., \"File\", \"PostgreSQL\").</param>
    /// <param name="level">The log level to verify (e.g., Information, Warning, Error).</param>
    /// <returns>True if the log level is enabled for the specified sink; otherwise, false.</returns>
    public static bool IsSinkLevelMatch(this SerilogConfiguration config, string sink, LogEventLevel level) {
        var response = config?.SerilogCondition?.Any(c => c.Sink == sink && c.Level.Contains(level.ToString())) == true;
        return response;
    }
}