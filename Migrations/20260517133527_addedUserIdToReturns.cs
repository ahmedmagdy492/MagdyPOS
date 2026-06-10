using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MagdyPOS.Migrations
{
    /// <inheritdoc />
    public partial class addedUserIdToReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Return",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Return_UserId",
                table: "Return",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Return_AspNetUsers_UserId",
                table: "Return",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Return_AspNetUsers_UserId",
                table: "Return");

            migrationBuilder.DropIndex(
                name: "IX_Return_UserId",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Return");
        }
    }
}
