using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
/// <summary>
/// Entity Framework Core DbContext for telemetry data.
/// Exposes DbSet properties for metrics, traces, and log entries.
/// </summary>
public class TelemetriesDbContext : DbContext {
    public DbSet<ViewHttpMetrics> ViewHttpMetrics => Set<ViewHttpMetrics>();
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
    /// <summary>
    /// Initializes a new instance of <see cref="TelemetriesDbContext"/> using the specified options.
    /// </summary>
    /// <param name="options">
    /// The <see cref="DbContextOptions{TelemetriesDbContext}"/> used to configure the context
    /// (e.g., the database provider and connection string).
    /// </param>
    public DbSet<LoggerTelemetryOptions> LoggerTelemetryOptions => Set<LoggerTelemetryOptions>();
    public TelemetriesDbContext(DbContextOptions<TelemetriesDbContext> options)
        : base(options) { }
    /// <summary>
    /// Configures the EF model by applying entity configuration classes for metrics, traces, and logs.
    /// </summary>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> used to construct the EF model.
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}