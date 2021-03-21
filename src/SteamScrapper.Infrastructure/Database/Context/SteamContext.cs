using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Infrastructure.Database.Entities;

namespace SteamScrapper.Infrastructure.Database.Context
{
    public class SteamContext : DbContext
    {
        private const int BannerUrlMaxLength = 2048;
        private const int TitleMaxLength = 2048;

        private const string AppNameDefaultValueSql = "N'Unknown App'";
        private const string BundleNameDefaultValueSql = "N'Unknown Bundle'";
        private const string SubNameDefaultValueSql = "N'Unknown Sub'";

        private const string BooleanFalseValueSql = "0";
        private const string SystemUtcDateTimeValueSql = "SYSUTCDATETIME()";
        private const string UtcDateTimeLastModifiedDefaultValueSql = "CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')";

        public SteamContext(DbContextOptions<SteamContext> dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<App> Apps => Set<App>();

        public DbSet<Bundle> Bundles => Set<Bundle>();

        public DbSet<Sub> Subs => Set<Sub>();

        public async Task<int> RegisterUnknownBundlesAsync(IEnumerable<int> bundleIds)
        {
            if (bundleIds is null)
            {
                throw new ArgumentNullException(nameof(bundleIds));
            }

            using var sqlCommand = Apps.CreateDbCommand();

            if (sqlCommand.Connection.State == ConnectionState.Closed || sqlCommand.Connection.State == ConnectionState.Broken)
            {
                await sqlCommand.Connection.OpenAsync();
            }

            var commandTexts = bundleIds
                .Distinct()
                .Select(bundleId => IncludeInsertUnknownBundle(sqlCommand, bundleId))
                .ToList();

            if (commandTexts.Count == 0)
            {
                return 0;
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
        }

        public async Task<int> RegisterUnknownSubsAsync(IEnumerable<int> subIds)
        {
            if (subIds is null)
            {
                throw new ArgumentNullException(nameof(subIds));
            }

            using var sqlCommand = Apps.CreateDbCommand();

            if (sqlCommand.Connection.State == ConnectionState.Closed || sqlCommand.Connection.State == ConnectionState.Broken)
            {
                await sqlCommand.Connection.OpenAsync();
            }

            var commandTexts = subIds
                .Distinct()
                .Select(subId => IncludeInsertUnknownSub(sqlCommand, subId))
                .ToList();

            if (commandTexts.Count == 0)
            {
                return 0;
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
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

        private static string IncludeInsertUnknownBundle(DbCommand command, long bundleId)
        {
            var parameter = command.CreateParameter();
            var parameterName = $"bundle_{bundleId}";

            parameter.ParameterName = parameterName;
            parameter.Value = bundleId;
            parameter.DbType = DbType.Int64;
            parameter.Direction = ParameterDirection.Input;

            command.Parameters.Add(parameter);

            return
                $"IF NOT EXISTS (SELECT 1 FROM [dbo].[Bundles] AS [B] WHERE [B].[Id] = @{parameterName}) " +
                $"INSERT INTO [dbo].[Bundles] ([Id]) VALUES (@{parameterName})";
        }

        private static string IncludeInsertUnknownSub(DbCommand command, long subId)
        {
            var parameter = command.CreateParameter();
            var parameterName = $"sub_{subId}";

            parameter.ParameterName = parameterName;
            parameter.Value = subId;
            parameter.DbType = DbType.Int64;
            parameter.Direction = ParameterDirection.Input;

            command.Parameters.Add(parameter);

            return
                $"IF NOT EXISTS (SELECT 1 FROM [dbo].[Subs] AS [S] WHERE [S].[Id] = @{parameterName}) " +
                $"INSERT INTO [dbo].[Subs] ([Id]) VALUES (@{parameterName})";
        }
    }
}