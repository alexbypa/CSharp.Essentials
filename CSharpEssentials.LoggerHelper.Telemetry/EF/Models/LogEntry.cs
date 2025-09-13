using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
[Table("LogEntry")]
public class LogEntry {
    public long Id { get; set; }
    public string ApplicationName { get; set; }
    public string message { get; set; }
    public string message_template { get; set; }
    public string level { get; set; }
    public DateTimeOffset? raise_date { get; set; }
    public string? exception { get; set; }
    public string? properties { get; set; }
    public string? props_test { get; set; }
    public string MachineName { get; set; }
    public string Action { get; set; }
    public string IdTransaction { get; set; }
}
public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry> {
    public void Configure(EntityTypeBuilder<LogEntry> builder) {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();
    }
}