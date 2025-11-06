using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WrikeTimeLogger.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WrikeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Template = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceContext = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LogLevel = table.Column<int>(type: "int", maxLength: 4000, nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    WrikeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExpiresIn = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsLoggedOut = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.WrikeId);
                });

            migrationBuilder.CreateTable(
                name: "Webhooks",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idempotencyKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    oldStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    oldCustomStatusId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    customStatusId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    taskId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    webhookId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    eventAuthorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    eventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    lastUpdatedDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    addedResponsibles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    timeTrackerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    hours = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhooks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TimeTrackers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    taskId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    hours = table.Column<double>(type: "float", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    timeTrackerId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeTrackers", x => x.id);
                    table.ForeignKey(
                        name: "FK_TimeTrackers_Users_userId",
                        column: x => x.userId,
                        principalTable: "Users",
                        principalColumn: "WrikeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsersTasks",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    Hours = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOut = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersTasks", x => new { x.UserId, x.TaskId });
                    table.ForeignKey(
                        name: "FK_UsersTasks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "WrikeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoursToAdd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WrikeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Hours = table.Column<double>(type: "float", nullable: false),
                    DateIn = table.Column<DateOnly>(type: "date", nullable: false),
                    DateUp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsersTasksTaskId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    UsersTasksUserId = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoursToAdd", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoursToAdd_UsersTasks_UsersTasksUserId_UsersTasksTaskId",
                        columns: x => new { x.UsersTasksUserId, x.UsersTasksTaskId },
                        principalTable: "UsersTasks",
                        principalColumns: new[] { "UserId", "TaskId" });
                    table.ForeignKey(
                        name: "FK_HoursToAdd_UsersTasks_WrikeId_TaskId",
                        columns: x => new { x.WrikeId, x.TaskId },
                        principalTable: "UsersTasks",
                        principalColumns: new[] { "UserId", "TaskId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HoursToAdd_UsersTasksUserId_UsersTasksTaskId",
                table: "HoursToAdd",
                columns: new[] { "UsersTasksUserId", "UsersTasksTaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_HoursToAdd_WrikeId_TaskId",
                table: "HoursToAdd",
                columns: new[] { "WrikeId", "TaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeTrackers_userId",
                table: "TimeTrackers",
                column: "userId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "HoursToAdd");

            migrationBuilder.DropTable(
                name: "TimeTrackers");

            migrationBuilder.DropTable(
                name: "Webhooks");

            migrationBuilder.DropTable(
                name: "UsersTasks");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
