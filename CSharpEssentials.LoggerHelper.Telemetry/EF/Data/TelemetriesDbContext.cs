using Microsoft.EntityFrameworkCore;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;

public class TelemetriesDbContext : DbContext {
    public DbSet<MetricEntry> Metrics => Set<MetricEntry>();
    public DbSet<TraceEntry> TraceEntry => Set<TraceEntry>();
    public DbSet<LogEntry> LogEntry => Set<LogEntry>();
    public TelemetriesDbContext(DbContextOptions<TelemetriesDbContext> options)
        : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfiguration(new TraceEntryConfiguration());
        new MetricEntryConfiguration().Configure(modelBuilder.Entity<MetricEntry>());
        new TraceEntryConfiguration().Configure(modelBuilder.Entity<TraceEntry>());
        new LogEntryConfiguration().Configure(modelBuilder.Entity<LogEntry>());
    }
}