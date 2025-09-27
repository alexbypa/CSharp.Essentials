using CSharpEssentials.LoggerHelper.Telemetry.Tracing;
using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Context;
// Implementazione concreta del contesto
public class LogTraceContext<T> : ILogTraceContext<T>, IDisposable {
    private readonly T _request;
    private Activity? _activity;
    public LogTraceContext(T request) {
        _request = request;

        ActivitySource.AddActivityListener(new ActivityListener {
            ShouldListenTo = source => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        });
    }
    public string TraceId { get; set; }
    public ILogTraceContext<T> AddTag(string key, object value) {
        if (_activity is not null)
            _activity.SetTag(key, value);
        return this;
    }
    public void Dispose() {
        StopActivity();
    }
    public ILogTraceContext<T> StartActivity(string Name) { 
        _activity = LoggerTelemetryActivitySource.Instance.StartActivity(
            Name, 
            ActivityKind.Internal,
            Activity.Current?.Context ?? default
        );
        if (_activity != null) {
            _activity.IsAllDataRequested = true;
            _activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            Console.WriteLine($"[TRACE] Started Activity: {_activity.DisplayName}");
            TraceId = _activity?.Context.TraceId.ToString();
        }
        Console.WriteLine($"[TRACE DEBUG] Activity created? {_activity != null}, name: {_activity?.DisplayName}");
        return this;
    }

    private void StopActivity() {
        _activity?.Stop();
    }
}