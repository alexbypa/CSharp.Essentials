using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;
public class RenderedMessageEnricher : ILogEventEnricher {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
        var bodyHtml = RenderEmailMessageHtml(logEvent);
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("Message", bodyHtml));

        if (logEvent == null || propertyFactory == null)
            return;
        var rendered = logEvent.RenderMessage(); // Elabora il messaggio finale con tutti gli args
        var property = propertyFactory.CreateProperty("RenderedMessage", rendered);
        logEvent.AddOrUpdateProperty(property);
    }
    private static string RenderEmailMessageHtml(LogEvent logEvent) {
        var rawMessage = logEvent.RenderMessage();

        // Qui puoi costruire il tuo HTML come vuoi, esempio semplice:
        return $@"
            <html>
                <body>
                    <h2>LoggerHelper Notification</h2>
                    <p><strong>Timestamp:</strong> {logEvent.Timestamp:yyyy-MM-dd HH:mm:ss}</p>
                    <p><strong>Level:</strong> {logEvent.Level}</p>
                    <p><strong>Message:</strong> {System.Net.WebUtility.HtmlEncode(rawMessage)}</p>
                </body>
            </html>";
    }
}