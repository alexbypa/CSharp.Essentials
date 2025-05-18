using Microsoft.EntityFrameworkCore;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;

public class MetricsDbContext : DbContext {
    public DbSet<MetricEntry> Metrics => Set<MetricEntry>();

    public MetricsDbContext(DbContextOptions<MetricsDbContext> options)
        : base(options) { }
}