using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSharpEssentials.LoggerHelper.Telemetry.Migrations
{
    /// <inheritdoc />
    public partial class updateexceptionOnLogEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "exception",
                schema: "dbo",
                table: "LogEntry",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "exception",
                schema: "dbo",
                table: "LogEntry",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
