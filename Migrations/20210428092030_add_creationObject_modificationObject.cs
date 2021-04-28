using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MiniDemo.Migrations
{
    public partial class add_creationObject_modificationObject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTime",
                table: "Tests",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "Tests",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTime",
                table: "Tests",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifierId",
                table: "Tests",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationTime",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "LastModificationTime",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "LastModifierId",
                table: "Tests");
        }
    }
}
