using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteamScrapper.Infrastructure.Database.Migrations
{
    public partial class BundleAggregationDateIndexIncludeBundleId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BundleAggregations_UtcDateTimeRecorded",
                table: "BundleAggregations");

            migrationBuilder.CreateIndex(
                name: "IX_BundleAggregations_UtcDateTimeRecorded",
                table: "BundleAggregations",
                column: "UtcDateTimeRecorded")
                .Annotation("SqlServer:Include", new[] { "BundleId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BundleAggregations_UtcDateTimeRecorded",
                table: "BundleAggregations");

            migrationBuilder.CreateIndex(
                name: "IX_BundleAggregations_UtcDateTimeRecorded",
                table: "BundleAggregations",
                column: "UtcDateTimeRecorded");
        }
    }
}
