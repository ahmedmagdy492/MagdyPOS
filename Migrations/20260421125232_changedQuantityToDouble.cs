using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MagdyPOS.Migrations
{
    /// <inheritdoc />
    public partial class changedQuantityToDouble : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Quantity",
                table: "Products_Materials",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "Quantity",
                table: "Materials",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "AlertLimit",
                table: "Materials",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Products_Materials");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "Materials",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<int>(
                name: "AlertLimit",
                table: "Materials",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldDefaultValue: 0.0);
        }
    }
}
