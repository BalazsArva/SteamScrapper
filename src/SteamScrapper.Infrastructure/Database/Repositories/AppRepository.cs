using System;
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
    public class AppRepository : IAppWriteRepository, IAppQueryRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;
        private readonly IDateTimeProvider dateTimeProvider;

        public AppRepository(IDbContextFactory<SteamContext> dbContextFactory, IDateTimeProvider dateTimeProvider)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<int> RegisterUnknownAppsAsync(IEnumerable<long> appIds)
        {
            if (appIds is null)
            {
                throw new ArgumentNullException(nameof(appIds));
            }

            if (!appIds.Any())
            {
                return 0;
            }

            using var context = dbContextFactory.CreateDbContext();
            using var sqlCommand = await CreateSqlCommandAsync(context);

            var commandTexts = appIds
                .Distinct()
                .Select(appId => IncludeInsertUnknownApp(sqlCommand, appId))
                .ToList();

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
        }

        public async Task UpdateAppsAsync(IEnumerable<Domain.Repositories.Models.App> appData)
        {
            if (appData is null)
            {
                throw new ArgumentNullException(nameof(appData));
            }

            if (!appData.Any())
            {
                return;
            }

            using var context = dbContextFactory.CreateDbContext();
            using var patchBatch = new EntityPatchBatch<Entities.App, long>(context);

            foreach (var app in appData)
            {
                var batch = patchBatch
                    .ForEntity(app.AppId)
                    .Patch(x => x.IsActive, app.IsActive)
                    .Patch(x => x.Title, app.Title)
                    .Patch(x => x.BannerUrl, app.BannerUrl)
                    .Patch(x => x.UtcDateTimeLastModified, dateTimeProvider.UtcNow);
            }

            await patchBatch.ExecuteAsync();
        }

        public async Task<int> CountUnscannedAppsAsync()
        {
            var today = dateTimeProvider.UtcNow.Date;

            using var context = dbContextFactory.CreateDbContext();

            return await context.Apps.CountAsync(x => x.UtcDateTimeLastModified < today);
        }

        public async Task<IEnumerable<long>> GetAppIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection)
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
                .Apps
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

        private static string IncludeInsertUnknownApp(DbCommand command, long appId)
        {
            var parameter = command.CreateParameter();
            var parameterName = $"app_{appId}";

            parameter.ParameterName = parameterName;
            parameter.Value = appId;
            parameter.DbType = DbType.Int64;
            parameter.Direction = ParameterDirection.Input;

            command.Parameters.Add(parameter);

            return
                $"IF NOT EXISTS (SELECT 1 FROM [dbo].[Apps] AS [A] WHERE [A].[Id] = @{parameterName}) " +
                $"INSERT INTO [dbo].[Apps] ([Id]) VALUES (@{parameterName})";
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