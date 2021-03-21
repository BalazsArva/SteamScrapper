using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteamScrapper.Infrastructure.Database.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "App",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtcDateTimeRecorded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UtcDateTimeLastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    Title = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    BannerUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bundle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtcDateTimeRecorded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UtcDateTimeLastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    Title = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    BannerUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sub",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtcDateTimeRecorded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UtcDateTimeLastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    Title = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sub", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_App_IsActive",
                table: "App",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_App_UtcDateTimeLastModified",
                table: "App",
                column: "UtcDateTimeLastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Bundle_IsActive",
                table: "Bundle",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Bundle_UtcDateTimeLastModified",
                table: "Bundle",
                column: "UtcDateTimeLastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Sub_IsActive",
                table: "Sub",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Sub_UtcDateTimeLastModified",
                table: "Sub",
                column: "UtcDateTimeLastModified");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "App");

            migrationBuilder.DropTable(
                name: "Bundle");

            migrationBuilder.DropTable(
                name: "Sub");
        }
    }
}
