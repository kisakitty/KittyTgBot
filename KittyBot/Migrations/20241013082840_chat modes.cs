using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KittyBot.Migrations
{
    /// <inheritdoc />
    public partial class chatmodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "ResponseConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatMode",
                table: "ModelsAnalytics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatMode",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ResponseConfigs");

            migrationBuilder.DropColumn(
                name: "ChatMode",
                table: "ModelsAnalytics");

            migrationBuilder.DropColumn(
                name: "ChatMode",
                table: "Messages");
        }
    }
}
