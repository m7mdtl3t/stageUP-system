using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivuqeQRSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedEventId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AssignedEventId",
                table: "Users",
                column: "AssignedEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Events_AssignedEventId",
                table: "Users",
                column: "AssignedEventId",
                principalTable: "Events",
                principalColumn: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Events_AssignedEventId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AssignedEventId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedEventId",
                table: "Users");
        }
    }
}
