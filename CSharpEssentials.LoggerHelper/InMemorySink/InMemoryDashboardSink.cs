using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace CSharpEssentials.LoggerHelper.InMemorySink;
public class InMemoryDashboardSink : ILogEventSink {
    private static readonly List<LogEvent> _logEvents = new List<LogEvent>();
    private static readonly object _lock = new object();
    public void Emit(LogEvent logEvent) {
        lock (_lock) {
            _logEvents.Add(logEvent);
            if (_logEvents.Count > 5000) {
                _logEvents.RemoveAt(0);
            }
        }
    }
    public static object GetLogEvents() {
        lock (_lock) {
            return new List<LogEvent>(_logEvents)
                .Select(e => new {
                    msg = MessageOnly(e),
                    timestamp = e.Timestamp,
                    level = e.Level.ToString(),
                    exception = e.Exception?.ToString(),
                    action = PropString(e, "Action")
                })
                .OrderBy(a => a.timestamp)
                .ToList();
        }
    }

    private static string MessageOnly(LogEvent e) =>
    string.Concat(e.MessageTemplate.Tokens
        .OfType<TextToken>()
        .Select(t => t.Text)).Trim();

    private static string? PropString(LogEvent e, string name) =>
        e.Properties.TryGetValue(name, out var v) && v is ScalarValue sv
            ? sv.Value?.ToString()
            : null;
}