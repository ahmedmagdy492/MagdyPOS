using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MagdyPOS.Migrations
{
    /// <inheritdoc />
    public partial class addedStockMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Orders",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "Stock_Movements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityDelta = table.Column<double>(type: "REAL", nullable: false),
                    QuantityBefore = table.Column<double>(type: "REAL", nullable: true),
                    QuantityAfter = table.Column<double>(type: "REAL", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    ReferenceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stock_Movements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_CreatedAt",
                table: "Stock_Movements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_ItemType_ItemId_CreatedAt",
                table: "Stock_Movements",
                columns: new[] { "ItemType", "ItemId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stock_Movements");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
