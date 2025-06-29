using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models {
    /// <summary>
    /// model to view metrics
    /// </summary>
    [Table("view_http_metrics", Schema = "public")]
    public class ViewHttpMetrics {
        /// <summary>
        /// name of metric
        /// </summary>
        public string Name { get; set; } = default!;
        /// <summary>
        /// value of metric
        /// </summary>
        public double Value { get; set; }
        /// <summary>
        /// timestamp metric
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// additional Tag
        /// </summary>
        public string? TagsJson { get; set; }
        /// <summary>
        /// field to relate Logs, traces and metrics
        /// </summary>
        public string? TraceId { get; set; }
    }
    /// <summary>
    /// Configuration for EF
    /// </summary>
    public class ViewHttpMetricsConfiguration : IEntityTypeConfiguration<ViewHttpMetrics> {
        /// <summary>
        /// Configuration for EF
        /// </summary>
        public void Configure(EntityTypeBuilder<ViewHttpMetrics> builder) {
            builder.Property(e => e.TraceId).HasMaxLength(100);
            builder.HasNoKey();
        }
    }
}