using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
[Table("LoggerTelemetryOptions", Schema = "public")]
public class LoggerTelemetryOptions {
    public int Id { get; set; }
    public bool IsEnabled { get; set; }
    public string ConnectionString { get; set; }
    public bool MeterListenerIsEnabled { get; set; }
    public DateTime LastUpdated { get; set; }
}
public class LoggerTelemetryOptionsConfiguration : IEntityTypeConfiguration<LoggerTelemetryOptions> {
    public void Configure(EntityTypeBuilder<LoggerTelemetryOptions> builder) {
        builder.HasKey(e => e.Id);
    }
}