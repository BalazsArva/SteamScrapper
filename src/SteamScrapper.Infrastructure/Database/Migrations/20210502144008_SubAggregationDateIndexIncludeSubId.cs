using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteamScrapper.Infrastructure.Database.Migrations
{
    public partial class SubAggregationDateIndexIncludeSubId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubAggregations_UtcDateTimeRecorded",
                table: "SubAggregations");

            migrationBuilder.CreateIndex(
                name: "IX_SubAggregations_UtcDateTimeRecorded",
                table: "SubAggregations",
                column: "UtcDateTimeRecorded")
                .Annotation("SqlServer:Include", new[] { "SubId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubAggregations_UtcDateTimeRecorded",
                table: "SubAggregations");

            migrationBuilder.CreateIndex(
                name: "IX_SubAggregations_UtcDateTimeRecorded",
                table: "SubAggregations",
                column: "UtcDateTimeRecorded");
        }
    }
}
