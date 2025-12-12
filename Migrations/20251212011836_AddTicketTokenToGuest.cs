using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivuqeQRSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTokenToGuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TicketToken",
                table: "Guests",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketToken",
                table: "Guests");
        }
    }
}
