using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MagdyPOS.Migrations
{
    public partial class AddOriginalQuantityAndCreatedAtToMaterials : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Materials",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<double>(
                name: "OriginalQuantity",
                table: "Materials",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "OriginalQuantity",
                table: "Materials");
        }
    }
}
