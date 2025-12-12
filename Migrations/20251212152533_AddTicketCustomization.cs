using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivuqeQRSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TicketDateDisplay",
                table: "Events",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketLocationDisplay",
                table: "Events",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketMapUrl",
                table: "Events",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketTitle",
                table: "Events",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketDateDisplay",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TicketLocationDisplay",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TicketMapUrl",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TicketTitle",
                table: "Events");
        }
    }
}
