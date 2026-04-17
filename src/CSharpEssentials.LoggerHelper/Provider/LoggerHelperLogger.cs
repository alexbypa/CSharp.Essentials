using Microsoft.Extensions.Logging;
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

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) =>
        _logger.IsEnabled(MapLevel(logLevel));

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (!IsEnabled(logLevel))
            return;

        var serilogLevel = MapLevel(logLevel);
        var message = formatter(state, exception);

        var log = _logger;
        if (eventId.Id != 0)
            log = log.ForContext("EventId", eventId.Id)
                     .ForContext("EventName", eventId.Name);

        log.Write(serilogLevel, exception, message);
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
