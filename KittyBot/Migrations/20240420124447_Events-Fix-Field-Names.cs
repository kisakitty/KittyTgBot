using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KittyBot.Migrations
{
    /// <inheritdoc />
    public partial class EventsFixFieldNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "title",
                table: "Events",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "enabled",
                table: "Events",
                newName: "Enabled");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Events",
                newName: "Description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Events",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Enabled",
                table: "Events",
                newName: "enabled");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Events",
                newName: "description");
        }
    }
}
