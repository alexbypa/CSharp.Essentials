using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Manages per-sink throttling to prevent flooding destinations like Email or Telegram.
/// </summary>
public static class SinkThrottlingManager {
    private static readonly ConcurrentDictionary<string, DateTime> _lastSent = new();

    /// <summary>
    /// Returns true if enough time has elapsed since the last send for this sink.
    /// </summary>
    public static bool CanSend(string sinkName, TimeSpan interval) {
        var now = DateTime.UtcNow;
        var last = _lastSent.GetOrAdd(sinkName, DateTime.MinValue);

        if (interval > TimeSpan.Zero && (now - last) < interval)
            return false;

        _lastSent[sinkName] = now;
        return true;
    }
}
