using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
/// <summary>
/// model for traces
/// </summary>
[Table("TraceEntry", Schema = "public")]
public class TraceEntry {
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// TraceId
    /// </summary>
    public string TraceId { get; set; } = null!;
    /// <summary>
    /// SpanId
    /// </summary>
    public string SpanId { get; set; } = null!;
    /// <summary>
    /// ParentSpanId
    /// </summary>
    public string? ParentSpanId { get; set; }
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = null!;
    /// <summary>
    /// StartTime
    /// </summary>
    public DateTime StartTime { get; set; }
    /// <summary>
    /// EndTime
    /// </summary>
    public DateTime EndTime { get; set; }
    /// <summary>
    /// durationMs
    /// </summary>
    public double DurationMs { get; set; }
    /// <summary>
    /// Tags
    /// </summary>
    public string TagsJson { get; set; } = "{}";
}
/// <summary>
/// Configuration
/// </summary>
public class TraceEntryConfiguration : IEntityTypeConfiguration<TraceEntry> {
    /// <summary>
    /// Configuration
    /// </summary>
    /// <param name="builder"></param>
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