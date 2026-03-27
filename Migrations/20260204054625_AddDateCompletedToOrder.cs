using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleEcom.Migrations
{
    /// <inheritdoc />
    public partial class AddDateCompletedToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateCompleted",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCompleted",
                table: "Orders");
        }
    }
}
