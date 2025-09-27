namespace CSharpEssentials.LoggerHelper.Telemetry;
public interface ILogTraceContext<T> : IDisposable {
    ILogTraceContext<T> StartActivity(string name);
    ILogTraceContext<T> AddTag(string key, object value);
    public string TraceId { get; set; }
}