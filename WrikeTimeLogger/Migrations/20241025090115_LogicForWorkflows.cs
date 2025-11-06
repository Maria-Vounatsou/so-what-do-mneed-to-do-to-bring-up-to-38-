using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WrikeTimeLogger.Migrations
{
    /// <inheritdoc />
    public partial class LogicForWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOut",
                table: "UsersTasks");

            migrationBuilder.DropColumn(
                name: "Hours",
                table: "UsersTasks");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UsersTasks");

            migrationBuilder.DropColumn(
                name: "IsLoggedOut",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomated",
                table: "UsersTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Workflow",
                table: "UsersTasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SourceContext",
                table: "ErrorLogs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Exception",
                table: "ErrorLogs",
                type: "nvarchar(max)",
                maxLength: 6000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutomated",
                table: "UsersTasks");

            migrationBuilder.DropColumn(
                name: "Workflow",
                table: "UsersTasks");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOut",
                table: "UsersTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Hours",
                table: "UsersTasks",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "UsersTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLoggedOut",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "SourceContext",
                table: "ErrorLogs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Exception",
                table: "ErrorLogs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 6000,
                oldNullable: true);
        }
    }
}
