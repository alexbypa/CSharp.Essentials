using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Serilog sink that captures low-level logs into the ring buffer.
/// On Error/Fatal events, automatically flushes buffered context.
/// </summary>
internal sealed class ContextualLogSink : ILogEventSink {
    private readonly ContextualLogBuffer _buffer;
    private readonly LoggerHolder _loggerHolder;
    private readonly HashSet<LogEventLevel> _capturedLevels;

    internal ContextualLogSink(ContextualLogBuffer buffer, LoggerHolder loggerHolder, IEnumerable<LogEventLevel>? capturedLevels = null) {
        _buffer = buffer;
        _loggerHolder = loggerHolder;
        _capturedLevels = capturedLevels is not null
            ? new HashSet<LogEventLevel>(capturedLevels)
            : [LogEventLevel.Debug, LogEventLevel.Information, LogEventLevel.Warning];
    }

    public void Emit(LogEvent logEvent) {
        if (_capturedLevels.Contains(logEvent.Level)) {
            var sourceContext = logEvent.Properties.TryGetValue("SourceContext", out var sc)
                ? sc.ToString().Trim('"')
                : null;

            _buffer.Push(logEvent.Level, logEvent.RenderMessage(), sourceContext, logEvent.Timestamp.UtcDateTime);
        }

        // On Error or Fatal, flush the buffer context
        if (logEvent.Level >= LogEventLevel.Error && _loggerHolder.Logger is { } flushTarget) {
            var context = _buffer.FlushAndClear();
            if (context.Count > 0) {
                foreach (var entry in context) {
                    flushTarget
                        .ForContext("IsContextualHistory", true)
                        .ForContext("OriginalLevel", entry.Level.ToString())
                        .ForContext("OriginalSourceContext", entry.SourceContext ?? "Unknown")
                        .Write(LogEventLevel.Error,
                            "[Context before error] [{OriginalLevel}] {ContextMessage}",
                            entry.Level, entry.Message);
                }
            }
        }
    }
}

/// <summary>
/// Holds a deferred reference to the Serilog logger.
/// Allows the ContextualLogSink to flush context entries through the
/// built logger without a circular dependency at construction time.
/// </summary>
internal sealed class LoggerHolder {
    internal Serilog.ILogger? Logger { get; set; }
}
