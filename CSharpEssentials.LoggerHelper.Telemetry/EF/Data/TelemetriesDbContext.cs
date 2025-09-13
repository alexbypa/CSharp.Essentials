using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
/// <summary>
/// Entity Framework Core DbContext for telemetry data.
/// Exposes DbSet properties for metrics, traces, and log entries.
/// </summary>
public class TelemetriesDbContext : DbContext {
    /// <summary>
    /// Represents the collection of metric entries in the database.
    /// </summary>
    public DbSet<MetricEntry> Metrics => Set<MetricEntry>();
    /// <summary>
    /// Represents the collection of trace entries in the database.
    /// </summary>
    public DbSet<TraceEntry> TraceEntry => Set<TraceEntry>();
    /// <summary>
    /// Represents the collection of log entries in the database.
    /// </summary>
    public DbSet<LogEntry> LogEntry => Set<LogEntry>();
    public TelemetriesDbContext(DbContextOptions options)
        : base(options) { }
    /// <summary>
    /// Configures the EF model by applying entity configuration classes for metrics, traces, and logs.
    /// </summary>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> used to construct the EF model.
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        if (Database.IsNpgsql()) {
            modelBuilder.Entity<TraceEntry>()
                .Property(e => e.TagsJson)
                .HasColumnType("jsonb");
        } else if (Database.IsSqlServer()) {
            modelBuilder.Entity<TraceEntry>()
                .Property(e => e.TagsJson)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<TraceEntry>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_TraceEntry_TagsJson_IsJson",
                    "ISJSON([TagsJson]) = 1"));
        }
    }
}