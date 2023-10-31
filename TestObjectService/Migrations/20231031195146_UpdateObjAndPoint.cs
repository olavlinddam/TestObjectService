using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestObjectService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateObjAndPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SniffingPoint_TestObjects_TestObjectId",
                table: "SniffingPoint");

            migrationBuilder.DropColumn(
                name: "MachineNr",
                table: "TestObjects");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "TestObjects",
                newName: "ImagePath");

            migrationBuilder.AlterColumn<string>(
                name: "SerialNr",
                table: "TestObjects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "MachineId",
                table: "TestObjects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "TestObjectId",
                table: "SniffingPoint",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_SniffingPoint_TestObjects_TestObjectId",
                table: "SniffingPoint",
                column: "TestObjectId",
                principalTable: "TestObjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SniffingPoint_TestObjects_TestObjectId",
                table: "SniffingPoint");

            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "TestObjects");

            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "TestObjects",
                newName: "ImageUrl");

            migrationBuilder.AlterColumn<int>(
                name: "SerialNr",
                table: "TestObjects",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "MachineNr",
                table: "TestObjects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "TestObjectId",
                table: "SniffingPoint",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SniffingPoint_TestObjects_TestObjectId",
                table: "SniffingPoint",
                column: "TestObjectId",
                principalTable: "TestObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
