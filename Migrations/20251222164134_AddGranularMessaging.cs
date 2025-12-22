using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivuqeQRSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGranularMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkDescription",
                table: "Events",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketVisualMessage",
                table: "Events",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppMessage",
                table: "Events",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkDescription",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TicketVisualMessage",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "WhatsAppMessage",
                table: "Events");
        }
    }
}
