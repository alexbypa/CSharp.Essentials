using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSharpEssentials.LoggerHelper.Telemetry.Migrations {
    /// <inheritdoc />
    public partial class NomeMigration : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "ViewHttpMetrics",
                schema: "public",
                columns: table => new {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TagsJson = table.Column<string>(type: "text", nullable: true),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table => {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "ViewHttpMetrics",
                schema: "public");
        }
    }
}
