using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Manages per-sink throttling to prevent flooding destinations like Email or Telegram.
/// </summary>
public static class SinkThrottlingManager {
    private static readonly ConcurrentDictionary<string, DateTime> _lastSent = new();

    /// <summary>
    /// Returns true if enough time has elapsed since the last send for this sink.
    /// Thread-safe: uses compare-and-swap to prevent TOCTOU races where two concurrent
    /// callers could both pass the time check and both proceed to send.
    /// </summary>
    public static bool CanSend(string sinkName, TimeSpan interval) {
        if (interval <= TimeSpan.Zero)
            return true;

        var now = DateTime.UtcNow;

        while (true) {
            var last = _lastSent.GetOrAdd(sinkName, DateTime.MinValue);
            if (now - last < interval)
                return false;

            // Atomically claim the throttle slot; retry if another thread updated first.
            if (_lastSent.TryUpdate(sinkName, now, last))
                return true;
        }
    }
}
