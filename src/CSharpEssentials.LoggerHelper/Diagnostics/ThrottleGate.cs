using Serilog.Debugging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Decides whether a log event may pass a sink, enforcing a minimum interval between two passed events.
/// </summary>
public interface IThrottleGate {
    /// <summary>Returns true when the event may be sent; false when it must be dropped.</summary>
    bool TryPass(LogEvent logEvent);
}

/// <summary>
/// Per-route throttle: one instance per sink route, so two routes of the same sink never share a slot.
/// A null or non-positive interval never drops. Thread-safe and lock-free; never throws.
/// </summary>
public sealed class ThrottleGate : IThrottleGate {
    private readonly string _sinkName;
    private readonly TimeSpan _interval;
    private readonly TimeProvider _timeProvider;
    private const long NeverClaimed = long.MinValue;
    private long _last = NeverClaimed;

    /// <param name="sinkName">Sink name, used only in the SelfLog message.</param>
    /// <param name="interval">Minimum interval between passed events. Null or &lt;= 0 disables throttling.</param>
    /// <param name="timeProvider">Clock; defaults to <see cref="TimeProvider.System"/>.</param>
    public ThrottleGate(string sinkName, TimeSpan? interval, TimeProvider? timeProvider = null) {
        _sinkName = sinkName ?? throw new ArgumentNullException(nameof(sinkName));
        _interval = interval ?? TimeSpan.Zero;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public bool TryPass(LogEvent logEvent) {
        if (_interval <= TimeSpan.Zero)
            return true;

        while (true) {
            var last = Volatile.Read(ref _last);
            var now = _timeProvider.GetTimestamp();

            // The sentinel is checked explicitly: it must never reach GetElapsedTime (overflow).
            if (last != NeverClaimed && _timeProvider.GetElapsedTime(last, now) < _interval) {
                SelfLog.WriteLine($"{_sinkName} throttled: {logEvent.RenderMessage()}");
                return false;
            }

            // Atomically claim the slot; retry if another thread claimed first.
            if (Interlocked.CompareExchange(ref _last, now, last) == last)
                return true;
        }
    }
}
