using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivuqeQRSystem.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeAndBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "Events",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Events",
                type: "TEXT",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "Events",
                type: "TEXT",
                maxLength: 7,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seniors_Name",
                table: "Seniors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Seniors_PhoneNumber",
                table: "Seniors",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Seniors_ShareToken",
                table: "Seniors",
                column: "ShareToken");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_IsAttended",
                table: "Guests",
                column: "IsAttended");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_Name",
                table: "Guests",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_PhoneNumber",
                table: "Guests",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_TicketToken",
                table: "Guests",
                column: "TicketToken");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Date",
                table: "Events",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsActive",
                table: "Events",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seniors_Name",
                table: "Seniors");

            migrationBuilder.DropIndex(
                name: "IX_Seniors_PhoneNumber",
                table: "Seniors");

            migrationBuilder.DropIndex(
                name: "IX_Seniors_ShareToken",
                table: "Seniors");

            migrationBuilder.DropIndex(
                name: "IX_Guests_IsAttended",
                table: "Guests");

            migrationBuilder.DropIndex(
                name: "IX_Guests_Name",
                table: "Guests");

            migrationBuilder.DropIndex(
                name: "IX_Guests_PhoneNumber",
                table: "Guests");

            migrationBuilder.DropIndex(
                name: "IX_Guests_TicketToken",
                table: "Guests");

            migrationBuilder.DropIndex(
                name: "IX_Events_Date",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsActive",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "Events");
        }
    }
}
