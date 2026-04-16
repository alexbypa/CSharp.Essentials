namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Represents an error that occurred during sink initialization or log writing.
/// </summary>
public sealed class LogErrorEntry {
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string SinkName { get; init; } = "Unknown";
    public string ErrorMessage { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
    public string? ContextInfo { get; init; }
}
