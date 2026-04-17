using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// ILogger implementation that forwards to Serilog with full structured logging support.
/// </summary>
internal sealed class LoggerHelperLogger : ILogger {
    private readonly Serilog.ILogger _logger;

    internal LoggerHelperLogger(Serilog.ILogger logger) {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        if (state is IEnumerable<KeyValuePair<string, object?>> properties) {
            var disposables = properties
                .Where(p => p.Key != "{OriginalFormat}")
                .Select(p => LogContext.PushProperty(p.Key, p.Value, destructureObjects: true))
                .ToList();
            return new CompositeDisposable(disposables);
        }
        return LogContext.PushProperty("Scope", state, destructureObjects: true);
    }

    public bool IsEnabled(LogLevel logLevel) =>
        _logger.IsEnabled(MapLevel(logLevel));

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (!IsEnabled(logLevel))
            return;

        var serilogLevel = MapLevel(logLevel);

        var log = _logger;
        if (eventId.Id != 0)
            log = log.ForContext("EventId", eventId.Id)
                     .ForContext("EventName", eventId.Name);

        // When state carries structured properties (the standard MEL case), extract the
        // original message template and property values so Serilog can parse them properly.
        // This preserves named properties like {nome} = "Ciccio" in the log event.
        // Falling back to the rendered string only for custom ILogger.Log<TState> usages
        // that don't follow the standard MEL key/value convention.
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs) {
            var props = pairs.ToList();
            var template = props.FirstOrDefault(p => p.Key == "{OriginalFormat}").Value?.ToString()
                           ?? formatter(state, exception);
            var values = props
                .Where(p => p.Key != "{OriginalFormat}")
                .Select(p => p.Value)
                .ToArray();

            log.Write(serilogLevel, exception, template, values);
        } else {
            log.Write(serilogLevel, exception, formatter(state, exception));
        }
    }

    private static LogEventLevel MapLevel(LogLevel logLevel) => logLevel switch {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };
}

internal sealed class CompositeDisposable(List<IDisposable> disposables) : IDisposable {
    public void Dispose() {
        foreach (var d in disposables)
            d.Dispose();
    }
}
