using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataRecipient.Repository.SQL.Migrations
{
    public partial class RegistrationKeyUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Registration",
                table: "Registration");

            migrationBuilder.AlterColumn<Guid>(
               name: "DataHolderBrandId",
               table: "Registration",
               type: "uniqueidentifier",
               nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "Registration",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Registration",
                table: "Registration",
                columns: new[] { "ClientId", "DataHolderBrandId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Registration",
                table: "Registration");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "Registration",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Registration",
                table: "Registration",
                column: "ClientId");
        }
    }
}
