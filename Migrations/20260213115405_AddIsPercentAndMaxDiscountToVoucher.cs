using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleEcom.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPercentAndMaxDiscountToVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPercent",
                table: "Vouchers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscount",
                table: "Vouchers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPercent",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MaxDiscount",
                table: "Vouchers");
        }
    }
}
