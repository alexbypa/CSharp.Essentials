using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
public class TraceEntry {
    public Guid Id { get; set; } = Guid.NewGuid();

    public string TraceId { get; set; } = null!;
    public string SpanId { get; set; } = null!;
    public string? ParentSpanId { get; set; }

    public string Name { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMs { get; set; }

    public string TagsJson { get; set; } = "{}";
}
public class TraceEntryConfiguration : IEntityTypeConfiguration<TraceEntry> {
    public void Configure(EntityTypeBuilder<TraceEntry> builder) {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TraceId).IsRequired();
        builder.Property(e => e.SpanId).IsRequired();
        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.StartTime).IsRequired();
        builder.Property(e => e.EndTime).IsRequired();
        builder.Property(e => e.DurationMs).IsRequired();
        builder.Property(e => e.TagsJson).HasColumnType("jsonb");
    }
}