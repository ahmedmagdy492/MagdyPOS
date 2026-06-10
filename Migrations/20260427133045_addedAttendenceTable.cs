using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MagdyPOS.Migrations
{
    /// <inheritdoc />
    public partial class addedAttendenceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        migrationBuilder.CreateTable(
            name: "Attendance_Records",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                WorkDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                CheckInAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                CheckOutAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Attendance_Records", x => x.Id);
                table.ForeignKey(
                    name: "FK_Attendance_Records_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Attendance_Records_WorkDate",
            table: "Attendance_Records",
            column: "WorkDate");

        migrationBuilder.CreateIndex(
            name: "IX_Attendance_Records_UserId_WorkDate",
            table: "Attendance_Records",
            columns: new[] { "UserId", "WorkDate" },
            unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        migrationBuilder.DropTable(
            name: "Attendance_Records");
        }
    }
}
