using OpenTelemetry;
using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Telemetry;
// Implementazione concreta del contesto
public class LogTraceContext<T> : ILogTraceContext<T> {
    private readonly T _request;
    private Activity? _activity;

    public LogTraceContext(T request) {
        _request = request;
    }

    public ILogTraceContext<T> AddBaggage(string key, string value) {
        Baggage.SetBaggage(key, value);
        return this;
    }

    public ILogTraceContext<T> AddTag(string key, object value) {
        if (_activity is not null)
            _activity.SetTag(key, value);
        return this;
    }

    public ILogTraceContext<T> StartActivity(string Name) {
        var activity = LoggerTelemetryActivitySource.Instance.StartActivity(Name);
        activity.Start();
        return this;
    }
}
