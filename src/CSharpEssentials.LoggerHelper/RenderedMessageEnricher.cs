using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Enricher that adds the rendered log message as a "RenderedMessage" property.
/// Useful for sinks that need the fully formatted message (e.g., database columns).
/// </summary>
public sealed class RenderedMessageEnricher : ILogEventEnricher {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
        var rendered = logEvent.RenderMessage();
        var property = propertyFactory.CreateProperty("RenderedMessage", rendered);
        logEvent.AddOrUpdateProperty(property);
    }
}
