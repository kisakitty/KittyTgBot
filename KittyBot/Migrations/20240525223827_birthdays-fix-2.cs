using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KittyBot.Migrations
{
    /// <inheritdoc />
    public partial class birthdaysfix2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Birthdays_Users_UserID",
                table: "Birthdays");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Birthdays",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Birthdays_UserID",
                table: "Birthdays",
                newName: "IX_Birthdays_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Birthdays_Users_UserId",
                table: "Birthdays",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Birthdays_Users_UserId",
                table: "Birthdays");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Birthdays",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Birthdays_UserId",
                table: "Birthdays",
                newName: "IX_Birthdays_UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Birthdays_Users_UserID",
                table: "Birthdays",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
