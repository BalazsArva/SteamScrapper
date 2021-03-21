using Microsoft.EntityFrameworkCore;
using SteamScrapper.Infrastructure.Database.Entities;

namespace SteamScrapper.Infrastructure.Database
{
    public class SteamContext : DbContext
    {
        private const int BannerUrlMaxLength = 2048;
        private const int TitleMaxLength = 2048;

        private const string BooleanFalseValueSql = "0";
        private const string SystemUtcDateTimeValueSql = "SYSUTCDATETIME()";
        private const string UtcDateTimeLastModifiedDefaultValueSql = "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')";

        public SteamContext(DbContextOptions<SteamContext> dbContextOptions)
            : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            SetupAppsTable(modelBuilder);
            SetupBundlesTable(modelBuilder);
            SetupSubsTable(modelBuilder);
        }

        private static void SetupAppsTable(ModelBuilder modelBuilder)
        {
            // Key setup
            modelBuilder
                .Entity<App>()
                .HasKey(x => x.Id);

            // Column setup
            modelBuilder
                .Entity<App>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql(SystemUtcDateTimeValueSql);

            modelBuilder
                .Entity<App>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql(SystemUtcDateTimeValueSql);

            modelBuilder
                .Entity<App>()
                .Property(x => x.UtcDateTimeLastModified)
                .HasDefaultValueSql(UtcDateTimeLastModifiedDefaultValueSql);

            modelBuilder
                .Entity<App>()
                .Property(x => x.IsActive)
                .IsRequired(true)
                .HasDefaultValueSql(BooleanFalseValueSql);

            modelBuilder
                .Entity<App>()
                .Property(x => x.BannerUrl)
                .IsRequired(false)
                .HasMaxLength(BannerUrlMaxLength);

            modelBuilder
                .Entity<App>()
                .Property(x => x.Title)
                .IsRequired(true)
                .HasMaxLength(TitleMaxLength);

            // Index setup
            modelBuilder
                .Entity<App>()
                .HasIndex(x => x.IsActive);

            modelBuilder
                .Entity<App>()
                .HasIndex(x => x.UtcDateTimeLastModified);
        }

        private static void SetupBundlesTable(ModelBuilder modelBuilder)
        {
            // Key setup
            modelBuilder
                .Entity<Bundle>()
                .HasKey(x => x.Id);

            // Column setup
            modelBuilder
                .Entity<Bundle>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql(SystemUtcDateTimeValueSql);

            modelBuilder
                .Entity<Bundle>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql(SystemUtcDateTimeValueSql);

            modelBuilder
                .Entity<Bundle>()
                .Property(x => x.UtcDateTimeLastModified)
                .HasDefaultValueSql(UtcDateTimeLastModifiedDefaultValueSql);

            modelBuilder
                .Entity<Bundle>()
                .Property(x => x.IsActive)
                .IsRequired(true)
                .HasDefaultValueSql(BooleanFalseValueSql);

            modelBuilder
                .Entity<Bundle>()
                .Property(x => x.BannerUrl)
                .IsRequired(false)
                .HasMaxLength(BannerUrlMaxLength);

            modelBuilder
                .Entity<Bundle>()
                .Property(x => x.Title)
                .IsRequired(true)
                .HasMaxLength(TitleMaxLength);

            // Index setup
            modelBuilder
                .Entity<Bundle>()
                .HasIndex(x => x.IsActive);

            modelBuilder
                .Entity<Bundle>()
                .HasIndex(x => x.UtcDateTimeLastModified);
        }

        private static void SetupSubsTable(ModelBuilder modelBuilder)
        {
            // Key setup
            modelBuilder
                .Entity<Sub>()
                .HasKey(x => x.Id);

            // Column setup
            modelBuilder
                .Entity<Sub>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql(SystemUtcDateTimeValueSql);

            modelBuilder
                .Entity<Sub>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql(SystemUtcDateTimeValueSql);

            modelBuilder
                .Entity<Sub>()
                .Property(x => x.UtcDateTimeLastModified)
                .HasDefaultValueSql(UtcDateTimeLastModifiedDefaultValueSql);

            modelBuilder
                .Entity<Sub>()
                .Property(x => x.IsActive)
                .IsRequired(true)
                .HasDefaultValueSql(BooleanFalseValueSql);

            modelBuilder
                .Entity<Sub>()
                .Property(x => x.Title)
                .IsRequired(true)
                .HasMaxLength(TitleMaxLength);

            // Index setup
            modelBuilder
                .Entity<Sub>()
                .HasIndex(x => x.IsActive);

            modelBuilder
                .Entity<Sub>()
                .HasIndex(x => x.UtcDateTimeLastModified);
        }
    }
}