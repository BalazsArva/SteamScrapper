using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteamScrapper.Infrastructure.Database.Migrations
{
    public partial class AddBundleAggregation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BundleAggregations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BundleId = table.Column<long>(type: "bigint", nullable: false),
                    UtcDateTimeRecorded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleAggregations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundleAggregations_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BundleAggregations_BundleId",
                table: "BundleAggregations",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleAggregations_UtcDateTimeRecorded",
                table: "BundleAggregations",
                column: "UtcDateTimeRecorded");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BundleAggregations");
        }
    }
}
