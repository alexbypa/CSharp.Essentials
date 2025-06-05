using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models;

public class MetricEntry {
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string? TagsJson { get; set; }
    public string? TraceId { get; set; }
    public string? BucketsJson { get; set; }
    public string? BoundariesJson { get; set; }
}
public class MetricEntryConfiguration : IEntityTypeConfiguration<MetricEntry> {
    public void Configure(EntityTypeBuilder<MetricEntry> builder) {
        builder.Property(e => e.TraceId).HasMaxLength(100);
    }
}