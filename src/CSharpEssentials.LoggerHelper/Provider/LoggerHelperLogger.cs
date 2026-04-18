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
            // Collect pushed disposables. Typical scope = 2-3 properties
            // (IdTransaction + Action + optional SpanName).
            IDisposable? d0 = null, d1 = null, d2 = null;
            List<IDisposable>? overflow = null;
            int count = 0;

            foreach (var p in properties) {
                if (p.Key == "{OriginalFormat}")
                    continue;
                var pushed = LogContext.PushProperty(p.Key, p.Value, destructureObjects: true);
                switch (count) {
                    case 0: d0 = pushed; break;
                    case 1: d1 = pushed; break;
                    case 2: d2 = pushed; break;
                    default:
                        overflow ??= [d0!, d1!, d2!];
                        overflow.Add(pushed);
                        break;
                }
                count++;
            }

            if (overflow is not null)
                return new CompositeDisposable(overflow);

            return count switch {
                0 => null,
                1 => d0,
                2 => new Disposable2(d0!, d1!),
                _ => new Disposable3(d0!, d1!, d2!)
            };
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
        // MEL passes state as IReadOnlyList<KVP> — single pass, direct array allocation.
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
                // Direct allocation — for typical log calls (1-5 params) this is faster
                // than ArrayPool: Rent(N) returns size 16 minimum → forces copy anyway,
                // plus Rent/Return/clearArray overhead. Small arrays are GC-cheap.
                var values = new object?[valueCount];
                int idx = 0;
                for (int i = 0; i < props.Count; i++) {
                    if (props[i].Key != "{OriginalFormat}")
                        values[idx++] = props[i].Value;
                }
                log.Write(serilogLevel, exception, template, values);
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

/// <summary>LIFO disposable for exactly 2 properties (most common scope).</summary>
internal sealed class Disposable2(IDisposable d0, IDisposable d1) : IDisposable {
    public void Dispose() { d1.Dispose(); d0.Dispose(); }
}

/// <summary>LIFO disposable for exactly 3 properties (IdTransaction + Action + SpanName).</summary>
internal sealed class Disposable3(IDisposable d0, IDisposable d1, IDisposable d2) : IDisposable {
    public void Dispose() { d2.Dispose(); d1.Dispose(); d0.Dispose(); }
}

/// <summary>LIFO disposable for 4+ properties (rare).</summary>
internal sealed class CompositeDisposable(List<IDisposable> disposables) : IDisposable {
    public void Dispose() {
        for (int i = disposables.Count - 1; i >= 0; i--)
            disposables[i].Dispose();
    }
}
