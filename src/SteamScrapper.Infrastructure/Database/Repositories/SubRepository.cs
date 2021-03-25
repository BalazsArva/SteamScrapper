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
    public class SubRepository : ISubQueryRepository, ISubWriteRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;
        private readonly IDateTimeProvider dateTimeProvider;

        public SubRepository(IDbContextFactory<SteamContext> dbContextFactory, IDateTimeProvider dateTimeProvider)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<int> RegisterUnknownSubsAsync(IEnumerable<long> subIds)
        {
            if (subIds is null)
            {
                throw new ArgumentNullException(nameof(subIds));
            }

            if (!subIds.Any())
            {
                return 0;
            }

            using var context = dbContextFactory.CreateDbContext();
            using var sqlCommand = await CreateSqlCommandAsync(context);

            var commandTexts = subIds
                .Distinct()
                .Select(appId => IncludeInsertUnknownSub(sqlCommand, appId))
                .ToList();

            if (commandTexts.Count == 0)
            {
                return 0;
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
        }

        public async Task<int> CountUnscannedSubsAsync()
        {
            var today = dateTimeProvider.UtcNow.Date;

            using var context = dbContextFactory.CreateDbContext();

            return await context.Subs.CountAsync(x => x.UtcDateTimeLastModified < today);
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