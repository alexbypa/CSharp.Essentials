using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Enricher that adds the rendered log message as a property to the log event.
/// This allows capturing the final formatted message with all arguments resolved.
/// </summary>
public class RenderedMessageEnricher : ILogEventEnricher {
    /// <summary>
    /// Enriches the log event by adding a "RenderedMessage" property containing
    /// the fully formatted message string.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The factory to create log event properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
        if (logEvent == null || propertyFactory == null)
            return;
        var rendered = logEvent.RenderMessage(); 
        var property = propertyFactory.CreateProperty("RenderedMessage", rendered);
        logEvent.AddOrUpdateProperty(property);
    }
}