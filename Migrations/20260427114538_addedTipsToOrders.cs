using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MagdyPOS.Migrations
{
    /// <inheritdoc />
    public partial class addedTipsToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Tips",
                table: "Orders",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tips",
                table: "Orders");
        }
    }
}
