namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration;
public class LoggerTelemetryOptions {
    public string ConnectionString { get; set; }
    public bool? IsEnabled { get; set; }
    public bool MeterListenerIsEnabled { get; set; }
    public CustomExporter CustomExporter { get; set; }
}
public class CustomExporter {
    public int exportIntervalMilliseconds { get; set; }
    public int exportTimeoutMilliseconds { get; set; }
}