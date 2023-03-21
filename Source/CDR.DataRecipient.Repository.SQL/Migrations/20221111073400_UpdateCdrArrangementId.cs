using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataRecipient.Repository.SQL.Migrations
{
    public partial class UpdateCdrArrangementId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CdrArrangement",
                table: "CdrArrangement");

            migrationBuilder.AlterColumn<string>(
                name: "CdrArrangementId",
                table: "CdrArrangement",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CdrArrangement",
                table: "CdrArrangement",
                column: "CdrArrangementId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CdrArrangement",
                table: "CdrArrangement");

            migrationBuilder.AlterColumn<Guid>(
                name: "CdrArrangementId",
                table: "CdrArrangement",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CdrArrangement",
                table: "CdrArrangement",
                column: "CdrArrangementId");
        }
    }
}
