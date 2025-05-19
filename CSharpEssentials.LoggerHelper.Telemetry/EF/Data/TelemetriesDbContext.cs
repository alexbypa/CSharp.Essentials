using Microsoft.EntityFrameworkCore;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;

public class TelemetriesDbContext : DbContext {
    public DbSet<MetricEntry> Metrics => Set<MetricEntry>();
    public DbSet<TraceEntry> TraceEntry => Set<TraceEntry>();
    public TelemetriesDbContext(DbContextOptions<TelemetriesDbContext> options)
        : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfiguration(new TraceEntryConfiguration());
    }
}