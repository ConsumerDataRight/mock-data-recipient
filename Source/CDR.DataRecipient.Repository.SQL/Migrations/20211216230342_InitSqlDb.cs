using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CDR.DataRecipient.Repository.SQL.Migrations
{
    public partial class InitSqlDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CdrArrangement",
                columns: table => new
                {
                    CdrArrangementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonDocument = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdrArrangement", x => x.CdrArrangementId);
                });

            migrationBuilder.CreateTable(
                name: "DataHolderBrand",
                columns: table => new
                {
                    DataHolderBrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonDocument = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHolderBrand", x => x.DataHolderBrandId);
                });

            migrationBuilder.CreateTable(
                name: "Registration",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonDocument = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registration", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareProduct",
                columns: table => new
                {
                    SoftwareProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SoftwareProductName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoftwareProductDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LogoUri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RecipientBaseUri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RedirectUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    JwksUri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareProduct", x => x.SoftwareProductId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CdrArrangement");

            migrationBuilder.DropTable(
                name: "DataHolderBrand");

            migrationBuilder.DropTable(
                name: "Registration");

            migrationBuilder.DropTable(
                name: "SoftwareProduct");
        }
    }
}
