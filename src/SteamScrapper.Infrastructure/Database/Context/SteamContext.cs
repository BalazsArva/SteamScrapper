using Microsoft.EntityFrameworkCore;
using SteamScrapper.Infrastructure.Database.Entities;

namespace SteamScrapper.Infrastructure.Database.Context
{
    public class SteamContext : DbContext
    {
        private const int BannerUrlMaxLength = 2048;
        private const int TitleMaxLength = 2048;
        private const int CurrencyMaxLength = 16;

        private const string AppNameDefaultValueSql = "N'Unknown App'";
        private const string BundleNameDefaultValueSql = "N'Unknown Bundle'";
        private const string SubNameDefaultValueSql = "N'Unknown Sub'";

        private const string BooleanFalseValueSql = "0";
        private const string SystemUtcDateTimeValueSql = "SYSUTCDATETIME()";
        private const string UtcDateTimeLastModifiedDefaultValueSql = "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')";

        private const string AppsTableName = "Apps";
        private const string BundlesTableName = "Bundles";
        private const string SubsTableName = "Subs";
        private const string SubPricesTableName = "SubPrices";
        private const string SubAggregationsTableName = "SubAggregations";

        public SteamContext(DbContextOptions<SteamContext> dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<App> Apps => Set<App>();

        public DbSet<Bundle> Bundles => Set<Bundle>();

        public DbSet<Sub> Subs => Set<Sub>();

        public DbSet<SubPrice> SubPrices => Set<SubPrice>();

        public DbSet<SubAggregation> SubAggregations => Set<SubAggregation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            SetupAppsTable(modelBuilder);
            SetupBundlesTable(modelBuilder);
            SetupSubsTable(modelBuilder);
            SetupSubPricesTable(modelBuilder);
            SetupSubAggregationsTable(modelBuilder);
        }

        private static void SetupAppsTable(ModelBuilder modelBuilder)
        {
            // Table setup
            modelBuilder
                .Entity<App>()
                .ToTable(AppsTableName);

            // Key setup
            modelBuilder
                .Entity<App>()
                .HasKey(x => x.Id);

            // Column setup
            modelBuilder
                .Entity<App>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

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
                .HasDefaultValueSql(AppNameDefaultValueSql)
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
            // Table setup
            modelBuilder
                .Entity<Bundle>()
                .ToTable(BundlesTableName);

            // Key setup
            modelBuilder
                .Entity<Bundle>()
                .HasKey(x => x.Id);

            // Column setup
            modelBuilder
                .Entity<Bundle>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

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
                .HasDefaultValueSql(BundleNameDefaultValueSql)
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
            // Table setup
            modelBuilder
                .Entity<Sub>()
                .ToTable(SubsTableName);

            // Key setup
            modelBuilder
                .Entity<Sub>()
                .HasKey(x => x.Id);

            // Column setup
            modelBuilder
                .Entity<Sub>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

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
                .HasDefaultValueSql(SubNameDefaultValueSql)
                .HasMaxLength(TitleMaxLength);

            // Index setup
            modelBuilder
                .Entity<Sub>()
                .HasIndex(x => x.IsActive);

            modelBuilder
                .Entity<Sub>()
                .HasIndex(x => x.UtcDateTimeLastModified);
        }

        private static void SetupSubPricesTable(ModelBuilder modelBuilder)
        {
            // Table setup
            modelBuilder
                .Entity<SubPrice>()
                .ToTable(SubPricesTableName);

            // Key setup
            modelBuilder
                .Entity<SubPrice>()
                .HasKey(x => x.Id);

            // FK setup
            modelBuilder
                .Entity<SubPrice>()
                .HasOne(x => x.Sub)
                .WithMany(x => x.Prices);

            // Column setup
            modelBuilder
                .Entity<SubPrice>()
                .Property(x => x.Id)
                .UseIdentityColumn();

            modelBuilder
                .Entity<SubPrice>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql(SystemUtcDateTimeValueSql);

            modelBuilder
                .Entity<SubPrice>()
                .Property(x => x.Price)
                .IsRequired(true);

            modelBuilder
                .Entity<SubPrice>()
                .Property(x => x.DiscountPrice)
                .IsRequired(false);

            modelBuilder
                .Entity<SubPrice>()
                .Property(x => x.Currency)
                .IsRequired(true)
                .HasMaxLength(CurrencyMaxLength);

            // Index setup
            modelBuilder
                .Entity<SubPrice>()
                .HasIndex(x => x.UtcDateTimeRecorded);
        }

        private static void SetupSubAggregationsTable(ModelBuilder modelBuilder)
        {
            // Table setup
            modelBuilder
                .Entity<SubAggregation>()
                .ToTable(SubAggregationsTableName);

            // Key setup
            modelBuilder
                .Entity<SubAggregation>()
                .HasKey(x => x.Id);

            // FK setup
            modelBuilder
                .Entity<SubAggregation>()
                .HasOne(x => x.Sub)
                .WithMany(x => x.Aggregations);

            // Column setup
            modelBuilder
                .Entity<SubAggregation>()
                .Property(x => x.Id)
                .UseIdentityColumn();

            modelBuilder
                .Entity<SubAggregation>()
                .Property(x => x.UtcDateTimeRecorded)
                .HasDefaultValueSql(SystemUtcDateTimeValueSql);

            // Index setup
            modelBuilder
                .Entity<SubAggregation>()
                .HasIndex(x => x.UtcDateTimeRecorded);
        }
    }
}