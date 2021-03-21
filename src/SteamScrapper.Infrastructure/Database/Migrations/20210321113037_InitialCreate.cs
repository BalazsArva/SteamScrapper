using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteamScrapper.Infrastructure.Database.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Apps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UtcDateTimeRecorded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UtcDateTimeLastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    Title = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false, defaultValueSql: "N'Unknown App'"),
                    BannerUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bundles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UtcDateTimeRecorded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UtcDateTimeLastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    Title = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false, defaultValueSql: "N'Unknown Bundle'"),
                    BannerUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UtcDateTimeRecorded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UtcDateTimeLastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    Title = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false, defaultValueSql: "N'Unknown Sub'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Apps_IsActive",
                table: "Apps",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Apps_UtcDateTimeLastModified",
                table: "Apps",
                column: "UtcDateTimeLastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_IsActive",
                table: "Bundles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_UtcDateTimeLastModified",
                table: "Bundles",
                column: "UtcDateTimeLastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Subs_IsActive",
                table: "Subs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Subs_UtcDateTimeLastModified",
                table: "Subs",
                column: "UtcDateTimeLastModified");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Apps");

            migrationBuilder.DropTable(
                name: "Bundles");

            migrationBuilder.DropTable(
                name: "Subs");
        }
    }
}
