using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.EntityFrameworkCore;

namespace CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
public sealed class TelemetryDbContextSqlServer : TelemetriesDbContext {
    public TelemetryDbContextSqlServer(DbContextOptions<TelemetryDbContextSqlServer> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder b) {
        base.OnModelCreating(b);
        // tipi specifici SQL Server
        b.Entity<MetricEntry>().Property(x => x.Value).HasColumnType("float"); // double
        // JSON come testo:
        // b.Entity<MetricEntry>().Property(x => x.TagsJson).HasColumnType("nvarchar(max)");
    }
}