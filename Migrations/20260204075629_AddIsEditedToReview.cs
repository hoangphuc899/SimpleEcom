using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleEcom.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEditedToReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "Reviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "Reviews");
        }
    }
}
