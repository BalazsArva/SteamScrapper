using Microsoft.EntityFrameworkCore.Migrations;

namespace SteamScrapper.Infrastructure.Database.Migrations
{
    public partial class AddSubDiscountPrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                table: "SubPrices",
                type: "decimal(18,2)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "SubPrices");
        }
    }
}
