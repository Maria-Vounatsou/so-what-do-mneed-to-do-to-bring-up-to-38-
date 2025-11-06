using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WrikeTimeLogger.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "addedResponsibles",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "hours",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "timeTrackerId",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "type",
                table: "Webhooks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "addedResponsibles",
                table: "Webhooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hours",
                table: "Webhooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "timeTrackerId",
                table: "Webhooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "Webhooks",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
