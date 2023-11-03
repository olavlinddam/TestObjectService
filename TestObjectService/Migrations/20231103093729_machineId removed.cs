﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestObjectService.Migrations
{
    /// <inheritdoc />
    public partial class machineIdremoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "TestObjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MachineId",
                table: "TestObjects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
