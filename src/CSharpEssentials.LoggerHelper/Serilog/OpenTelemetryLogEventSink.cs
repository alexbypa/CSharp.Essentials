using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Bridges Serilog log events to System.Diagnostics.Activity events,
/// enabling OpenTelemetry trace correlation.
/// Each log event becomes an ActivityEvent on the current trace span.
/// </summary>
public sealed class OpenTelemetryLogEventSink : ILogEventSink {
    public void Emit(LogEvent logEvent) {
        var activity = Activity.Current;
        if (activity is null)
            return;

        var tags = new ActivityTagsCollection {
            { "log.level", logEvent.Level.ToString() },
            { "log.message", logEvent.RenderMessage() }
        };

        if (logEvent.Exception is not null) {
            tags.Add("exception.message", logEvent.Exception.Message);
            tags.Add("exception.stacktrace", logEvent.Exception.StackTrace ?? "");
        }

        activity.AddEvent(new ActivityEvent("log", logEvent.Timestamp.UtcDateTime, tags));
    }
}
