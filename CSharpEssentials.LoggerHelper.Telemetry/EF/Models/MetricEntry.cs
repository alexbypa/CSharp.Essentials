namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models;

public class MetricEntry {
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string? TagsJson { get; set; } 
}
