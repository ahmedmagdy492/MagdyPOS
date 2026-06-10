using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MagdyPOS.Migrations
{
    /// <inheritdoc />
    public partial class addedOrdersStateAndReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Return",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Return", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Return_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Return_OrderId",
                table: "Return",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Return");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Orders");
        }
    }
}
