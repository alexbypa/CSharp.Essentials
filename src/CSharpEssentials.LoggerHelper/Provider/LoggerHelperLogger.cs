using System.Buffers;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// ILogger implementation that forwards to Serilog with full structured logging support.
/// Optimized for minimal allocations on the hot path.
/// </summary>
internal sealed class LoggerHelperLogger : ILogger {
    private readonly Serilog.ILogger _logger;

    internal LoggerHelperLogger(Serilog.ILogger logger) {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        if (state is IEnumerable<KeyValuePair<string, object?>> properties) {
            // Count first to avoid List allocation for small scopes
            var disposables = new List<IDisposable>();
            foreach (var p in properties) {
                if (p.Key != "{OriginalFormat}")
                    disposables.Add(LogContext.PushProperty(p.Key, p.Value, destructureObjects: true));
            }
            return disposables.Count == 1 ? disposables[0] : new CompositeDisposable(disposables);
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

        // Extract structured properties without LINQ allocations.
        // MEL passes state as IReadOnlyList<KVP> — enumerate once, rent an array for values.
        if (state is IReadOnlyList<KeyValuePair<string, object?>> props) {
            string? template = null;
            int valueCount = 0;

            // First pass: find template and count values
            for (int i = 0; i < props.Count; i++) {
                if (props[i].Key == "{OriginalFormat}")
                    template = props[i].Value?.ToString();
                else
                    valueCount++;
            }

            template ??= formatter(state, exception);

            if (valueCount == 0) {
                log.Write(serilogLevel, exception, template);
            } else {
                // Rent from pool to avoid per-call array allocation
                var rented = ArrayPool<object?>.Shared.Rent(valueCount);
                try {
                    int idx = 0;
                    for (int i = 0; i < props.Count; i++) {
                        if (props[i].Key != "{OriginalFormat}")
                            rented[idx++] = props[i].Value;
                    }

                    // Serilog needs exact-length array; slice via Span to avoid copy
                    // when rented length matches. Otherwise we must copy.
                    if (rented.Length == valueCount) {
                        log.Write(serilogLevel, exception, template, rented);
                    } else {
                        var exact = new object?[valueCount];
                        Array.Copy(rented, exact, valueCount);
                        log.Write(serilogLevel, exception, template, exact);
                    }
                } finally {
                    ArrayPool<object?>.Shared.Return(rented, clearArray: true);
                }
            }
        } else if (state is IEnumerable<KeyValuePair<string, object?>> pairs) {
            // Fallback for non-list enumerables (rare)
            string? template = null;
            var values = new List<object?>();
            foreach (var p in pairs) {
                if (p.Key == "{OriginalFormat}")
                    template = p.Value?.ToString();
                else
                    values.Add(p.Value);
            }
            template ??= formatter(state, exception);
            log.Write(serilogLevel, exception, template, values.ToArray());
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
