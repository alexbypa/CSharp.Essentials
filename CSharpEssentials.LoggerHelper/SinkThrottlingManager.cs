using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper;
public static class SinkThrottlingManager {
    private static readonly ConcurrentDictionary<string, DateTime> _lastSent = new();
    public static bool CanSend(string sinkName, TimeSpan interval) {
        var now = DateTime.UtcNow;
        var last = _lastSent.GetOrAdd(sinkName, DateTime.MinValue);

        if ((now - last) < interval && interval > TimeSpan.FromSeconds(0))
            return false;

        _lastSent[sinkName] = now;
        return true;
    }
}