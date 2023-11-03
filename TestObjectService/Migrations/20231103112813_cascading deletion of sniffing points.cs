using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestObjectService.Migrations
{
    /// <inheritdoc />
    public partial class cascadingdeletionofsniffingpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SniffingPoint_TestObjects_TestObjectId",
                table: "SniffingPoint");

            migrationBuilder.AddForeignKey(
                name: "FK_SniffingPoint_TestObjects_TestObjectId",
                table: "SniffingPoint",
                column: "TestObjectId",
                principalTable: "TestObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SniffingPoint_TestObjects_TestObjectId",
                table: "SniffingPoint");

            migrationBuilder.AddForeignKey(
                name: "FK_SniffingPoint_TestObjects_TestObjectId",
                table: "SniffingPoint",
                column: "TestObjectId",
                principalTable: "TestObjects",
                principalColumn: "Id");
        }
    }
}
