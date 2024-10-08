﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KittyBot.Migrations
{
    /// <inheritdoc />
    public partial class activemembersinchats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Stats",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Stats");
        }
    }
}
