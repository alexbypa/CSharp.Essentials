using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.EntityFrameworkCore;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
public sealed class TelemetryDbContextNpgsql : TelemetriesDbContext {
    public TelemetryDbContextNpgsql(DbContextOptions<TelemetryDbContextNpgsql> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b) {
        base.OnModelCreating(b);
        b.Entity<MetricEntry>().Property(x => x.Value).HasColumnType("double precision");
    }
}
