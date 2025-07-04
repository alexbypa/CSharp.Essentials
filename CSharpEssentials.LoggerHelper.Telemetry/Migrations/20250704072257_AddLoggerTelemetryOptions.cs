using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CSharpEssentials.LoggerHelper.Telemetry.Migrations
{
    /// <inheritdoc />
    public partial class AddLoggerTelemetryOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ViewHttpMetrics",
                schema: "public",
                newName: "view_http_metrics",
                newSchema: "public");

            migrationBuilder.CreateTable(
                name: "LoggerTelemetryOptions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConnectionString = table.Column<string>(type: "text", nullable: false),
                    MeterListenerIsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoggerTelemetryOptions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoggerTelemetryOptions",
                schema: "public");

            migrationBuilder.RenameTable(
                name: "view_http_metrics",
                schema: "public",
                newName: "ViewHttpMetrics",
                newSchema: "public");
        }
    }
}
