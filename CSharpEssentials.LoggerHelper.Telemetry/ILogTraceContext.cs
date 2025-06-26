namespace CSharpEssentials.LoggerHelper.Telemetry;
public interface ILogTraceContext<T> {
    void StopActivity();
    ILogTraceContext<T> StartActivity(string name);
    ILogTraceContext<T> AddTag(string key, object value);
    ILogTraceContext<T> AddBaggage(string key, string value);
}