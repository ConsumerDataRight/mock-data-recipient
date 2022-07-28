using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataRecipient.Repository.SQL.Migrations
{
    public partial class DCR_Message : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DcrMessage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    DataHolderBrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MessageState = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    MessageError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DcrMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogEventsDcrService",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThreadId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceContext = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEventsDcrService", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DcrMessage");

            migrationBuilder.DropTable(
                name: "LogEventsDcrService");
        }
    }
}
