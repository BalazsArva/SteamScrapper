﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Contracts;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class BundleRepository : IBundleQueryRepository, IBundleWriteRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;
        private readonly IDateTimeProvider dateTimeProvider;

        public BundleRepository(IDbContextFactory<SteamContext> dbContextFactory, IDateTimeProvider dateTimeProvider)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        // TODO: BundleData is no longer a "service contract" (namespace!). Use some aggregate instead.
        public async Task UpdateBundlesAsync(IEnumerable<BundleData> bundleData)
        {
            if (bundleData is null)
            {
                throw new ArgumentNullException(nameof(bundleData));
            }

            if (!bundleData.Any())
            {
                return;
            }

            using var context = dbContextFactory.CreateDbContext();
            using var sqlCommand = await CreateSqlCommandAsync(context);

            var commandTexts = new List<string>();

            foreach (var bundle in bundleData)
            {
                commandTexts.Add(AddBundleDetailsToUpdateCommand(sqlCommand, bundle));
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async Task<int> RegisterUnknownBundlesAsync(IEnumerable<long> bundleIds)
        {
            if (bundleIds is null)
            {
                throw new ArgumentNullException(nameof(bundleIds));
            }

            if (!bundleIds.Any())
            {
                return 0;
            }

            using var context = dbContextFactory.CreateDbContext();
            using var sqlCommand = await CreateSqlCommandAsync(context);

            if (sqlCommand.Connection.State == ConnectionState.Closed || sqlCommand.Connection.State == ConnectionState.Broken)
            {
                await sqlCommand.Connection.OpenAsync();
            }

            var commandTexts = bundleIds
                .Distinct()
                .Select(bundleId => IncludeInsertUnknownBundle(sqlCommand, bundleId))
                .ToList();

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
        }

        public async Task<int> CountUnscannedBundlesAsync()
        {
            var today = dateTimeProvider.UtcNow.Date;

            using var context = dbContextFactory.CreateDbContext();

            return await context.Bundles.CountAsync(x => x.UtcDateTimeLastModified < today);
        }

        public async Task<IEnumerable<long>> GetBundleIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection)
        {
            if (page < 1)
            {
                throw new ArgumentException($"The value of the '{nameof(page)}' must be at least 1.", nameof(page));
            }

            if (pageSize < 1)
            {
                throw new ArgumentException($"The value of the '{nameof(pageSize)}' must be at least 1.", nameof(pageSize));
            }

            using var context = dbContextFactory.CreateDbContext();

            var filteredResults = context
                .Bundles
                .Where(x => x.UtcDateTimeLastModified < from)
                .Select(x => x.Id);

            if (sortDirection == SortDirection.Ascending)
            {
                filteredResults = filteredResults.OrderBy(x => x);
            }
            else
            {
                filteredResults = filteredResults.OrderByDescending(x => x);
            }

            return await filteredResults.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
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

        private async Task<DbCommand> CreateSqlCommandAsync(SteamContext context)
        {
            var command = context.Apps.CreateDbCommand();

            if (command.Connection.State == ConnectionState.Closed || command.Connection.State == ConnectionState.Broken)
            {
                await command.Connection.OpenAsync();
            }

            return command;
        }

        private static string AddBundleDetailsToUpdateCommand(DbCommand command, BundleData bundleData)
        {
            var bundleId = bundleData.BundleId;

            var idParameter = command.CreateParameter();
            var titleParameter = command.CreateParameter();
            var bannerUrlParameter = command.CreateParameter();
            var isActiveParameter = command.CreateParameter();

            var idParameterName = $"bundleId_{bundleId}";
            var titleParameterName = $"bundleTitle_{bundleId}";
            var bannerUrlParameterName = $"bundleBanner_{bundleId}";
            var isActiveParameterName = $"bundleIsActive_{bundleId}";

            idParameter.ParameterName = idParameterName;
            idParameter.Value = bundleId;
            idParameter.DbType = DbType.Int64;
            idParameter.Direction = ParameterDirection.Input;

            titleParameter.ParameterName = titleParameterName;
            titleParameter.Value = bundleData.Title;
            titleParameter.DbType = DbType.String;
            titleParameter.Direction = ParameterDirection.Input;

            bannerUrlParameter.ParameterName = bannerUrlParameterName;
            bannerUrlParameter.Value = bundleData.BannerUrl ?? (object)DBNull.Value;
            bannerUrlParameter.DbType = DbType.String;
            bannerUrlParameter.Direction = ParameterDirection.Input;

            isActiveParameter.ParameterName = isActiveParameterName;
            isActiveParameter.Value = bundleData.IsActive;
            isActiveParameter.DbType = DbType.Boolean;
            isActiveParameter.Direction = ParameterDirection.Input;

            command.Parameters.Add(idParameter);
            command.Parameters.Add(titleParameter);
            command.Parameters.Add(bannerUrlParameter);
            command.Parameters.Add(isActiveParameter);

            return string.Concat(
                $"UPDATE [dbo].[Bundles] ",
                $"SET [Title] = @{titleParameterName}, [BannerUrl] = @{bannerUrlParameterName}, [UtcDateTimeLastModified] = SYSUTCDATETIME(), [IsActive] = @{isActiveParameterName} ",
                $"WHERE [Id] = @{idParameterName}");
        }
    }
}