using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivuqeQRSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Seniors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Seniors_EventId",
                table: "Seniors",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Seniors_Events_EventId",
                table: "Seniors",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seniors_Events_EventId",
                table: "Seniors");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Seniors_EventId",
                table: "Seniors");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Seniors");
        }
    }
}
