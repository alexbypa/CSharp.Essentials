using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;
public class RenderedMessageEnricher : ILogEventEnricher {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
        if (logEvent == null || propertyFactory == null)
            return;
        var rendered = logEvent.RenderMessage(); // Elabora il messaggio finale con tutti gli args
        var property = propertyFactory.CreateProperty("RenderedMessage", rendered);
        logEvent.AddOrUpdateProperty(property);
    }
}
