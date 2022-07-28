using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataRecipient.Repository.SQL.Migrations
{
    public partial class UpdateDcrMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "DcrMessage",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandName",
                table: "DcrMessage",
                type: "nvarchar(1000)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "DcrMessage");

            migrationBuilder.DropColumn(
                name: "BrandName",
                table: "DcrMessage");
        }
    }
}
