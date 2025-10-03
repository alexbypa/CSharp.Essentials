using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models;

[Table("MetricEntry")]
public class MetricEntry {
    public int Id { get; set; } 
    public string Name { get; set; } = default!;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string? TagsJson { get; set; }
    public string? TraceId { get; set; }
}
public class MetricEntryConfiguration : IEntityTypeConfiguration<MetricEntry> {
    public void Configure(EntityTypeBuilder<MetricEntry> builder) {
        builder.Property(e => e.TraceId).HasMaxLength(100);
    }
}