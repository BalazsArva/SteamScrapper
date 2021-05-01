using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteamScrapper.Infrastructure.Database.Migrations
{
    public partial class AddBundlePrices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BundlePrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BundleId = table.Column<long>(type: "bigint", nullable: false),
                    UtcDateTimeRecorded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundlePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundlePrices_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BundlePrices_BundleId",
                table: "BundlePrices",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_BundlePrices_UtcDateTimeRecorded",
                table: "BundlePrices",
                column: "UtcDateTimeRecorded");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BundlePrices");
        }
    }
}
