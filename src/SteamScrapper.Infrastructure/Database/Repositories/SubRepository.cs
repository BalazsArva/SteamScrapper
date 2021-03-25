using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class SubRepository : ISubRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;

        public SubRepository(IDbContextFactory<SteamContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
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