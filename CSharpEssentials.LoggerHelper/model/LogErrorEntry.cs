namespace CSharpEssentials.LoggerHelper.model;
public class LogErrorEntry {
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SinkName { get; set; } = "Unknown";
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? ContextInfo { get; set; } // es. file path, db name, endpoint, etc.
}