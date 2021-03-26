﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
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
    }
}