using CSharpEssentials.LoggerHelper;
using Serilog;

namespace Test.Controllers.logger;

/// <summary>
/// Custom enricher that dynamically adds common properties to the logging context.
/// Can be used from middleware, controllers, or anywhere a contextual logger is needed.
/// </summary>
public class MyCustomEnricher : IContextLogEnricher {
    public Serilog.ILogger Enrich(Serilog.ILogger logger, object? context) {
        if (context == null)
            return logger;

        var props = new Dictionary<string, string>();
        var type = context.GetType();

        var user = type.GetProperty("Username")?.GetValue(context)?.ToString();
        var ip = type.GetProperty("IpAddress")?.GetValue(context)?.ToString();

        if (!string.IsNullOrEmpty(user))
            props["Username"] = user!;
        if (!string.IsNullOrEmpty(ip))
            props["IpAddress"] = ip!;

        foreach (var kvp in props) {
            logger = logger.ForContext(kvp.Key, kvp.Value);
        }

        return logger;
    }
    /// <summary>
    /// Enriches the global LoggerConfiguration with static properties that will be included in every log event,
    /// regardless of the context.
    /// </summary>
    /// <param name="configuration">The Serilog LoggerConfiguration instance.</param>
    /// <returns>The enriched LoggerConfiguration instance.</returns>
    public LoggerConfiguration Enrich(LoggerConfiguration configuration) {
        return configuration;
    }
}