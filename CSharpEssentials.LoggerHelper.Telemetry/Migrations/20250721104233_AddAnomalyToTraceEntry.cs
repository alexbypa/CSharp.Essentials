using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSharpEssentials.LoggerHelper.Telemetry.Migrations
{
    /// <inheritdoc />
    public partial class AddAnomalyToTraceEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Anomaly",
                schema: "public",
                table: "TraceEntry",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anomaly",
                schema: "public",
                table: "TraceEntry");
        }
    }
}
