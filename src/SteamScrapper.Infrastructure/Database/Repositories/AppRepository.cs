using System;
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
    public class AppRepository : IAppWriteRepository, IAppQueryRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;
        private readonly IDateTimeProvider dateTimeProvider;

        public AppRepository(IDbContextFactory<SteamContext> dbContextFactory, IDateTimeProvider dateTimeProvider)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<int> RegisterUnknownAppsAsync(IEnumerable<int> appIds)
        {
            if (appIds is null)
            {
                throw new ArgumentNullException(nameof(appIds));
            }

            using var context = dbContextFactory.CreateDbContext();

            return await context.RegisterUnknownAppsAsync(appIds);
        }

        public async Task UpdateAppsAsync(IEnumerable<AppData> appData)
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

            using var sqlCommand = await CreateSqlCommandAsync(context);

            var commandTexts = new List<string>();

            foreach (var app in appData)
            {
                commandTexts.Add(AddAppDetailsToUpdateCommand(sqlCommand, app));
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async Task<int> CountUnscannedAppsAsync()
        {
            var now = dateTimeProvider.UtcNow;

            using var context = dbContextFactory.CreateDbContext();

            return await context.Apps.CountAsync(x => x.UtcDateTimeLastModified < now);
        }

        private static string AddAppDetailsToUpdateCommand(DbCommand command, AppData appData)
        {
            var appId = appData.AppId;

            var idParameterName = $"appId_{appId}";
            var isActiveParameterName = $"isActive_{appId}";
            var titleParameterName = $"appTitle_{appId}";
            var bannerUrlParameterName = $"appBanner_{appId}";

            var idParameter = command.CreateParameter();
            var isActiveParameter = command.CreateParameter();
            var titleParameter = command.CreateParameter();
            var bannerUrlParameter = command.CreateParameter();

            idParameter.ParameterName = idParameterName;
            idParameter.Value = appId;
            idParameter.DbType = DbType.Int64;
            idParameter.Direction = ParameterDirection.Input;

            titleParameter.ParameterName = titleParameterName;
            titleParameter.Value = appData.Title;
            titleParameter.DbType = DbType.String;
            titleParameter.Direction = ParameterDirection.Input;

            bannerUrlParameter.ParameterName = bannerUrlParameterName;
            bannerUrlParameter.Value = appData.BannerUrl ?? (object)DBNull.Value;
            bannerUrlParameter.DbType = DbType.String;
            bannerUrlParameter.Direction = ParameterDirection.Input;

            isActiveParameter.ParameterName = isActiveParameterName;
            isActiveParameter.Value = appData.IsActive;
            isActiveParameter.DbType = DbType.Boolean;
            isActiveParameter.Direction = ParameterDirection.Input;

            command.Parameters.Add(idParameter);
            command.Parameters.Add(isActiveParameter);
            command.Parameters.Add(titleParameter);
            command.Parameters.Add(bannerUrlParameter);

            return string.Concat(
                $"UPDATE [dbo].[Apps] ",
                $"SET [Title] = @{titleParameterName}, [IsActive] = @{isActiveParameterName}, [BannerUrl] = @{bannerUrlParameterName}, [UtcDateTimeLastModified] = SYSUTCDATETIME() ",
                $"WHERE [Id] = @{idParameterName}");
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