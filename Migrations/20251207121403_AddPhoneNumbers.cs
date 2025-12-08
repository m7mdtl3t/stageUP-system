using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivuqeQRSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Seniors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Guests",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Seniors");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Guests");
        }
    }
}
