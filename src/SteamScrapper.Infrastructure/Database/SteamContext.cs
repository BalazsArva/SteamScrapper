using Microsoft.EntityFrameworkCore;
using SteamScrapper.Infrastructure.Database.Entities;

namespace SteamScrapper.Infrastructure.Database
{
    public class SteamContext : DbContext
    {
        public SteamContext(DbContextOptions<SteamContext> dbContextOptions)
            : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<App>()
                .HasKey(x => x.Id);

            modelBuilder
                .Entity<App>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            modelBuilder
                .Entity<App>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            modelBuilder
                .Entity<App>()
                .Property(x => x.UtcDateTimeLastModified)
                .HasDefaultValueSql("CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')");
        }
    }
}