using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models {
    [Table("view_http_metrics", Schema = "public")]
    public class ViewHttpMetrics {
        public string Name { get; set; } = default!;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TagsJson { get; set; }
        public string? TraceId { get; set; }
    }
    public class ViewHttpMetricsConfiguration : IEntityTypeConfiguration<ViewHttpMetrics> {
        public void Configure(EntityTypeBuilder<ViewHttpMetrics> builder) {
            builder.Property(e => e.TraceId).HasMaxLength(100);
            builder.HasNoKey();
        }
    }
}
