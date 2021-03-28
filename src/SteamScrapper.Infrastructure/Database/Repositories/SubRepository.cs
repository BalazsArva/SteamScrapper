using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Repositories.Models;
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

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
        }

        public async Task UpdateSubsAsync(IEnumerable<Sub> subData)
        {
            if (subData is null)
            {
                throw new ArgumentNullException(nameof(subData));
            }

            if (!subData.Any())
            {
                return;
            }

            using var context = dbContextFactory.CreateDbContext();
            using var sqlCommand = await CreateSqlCommandAsync(context);

            var commandTexts = new List<string>();

            foreach (var sub in subData)
            {
                commandTexts.Add(AddSubDetailsToCommand(sqlCommand, sub));

                if (sub.Price is not null)
                {
                    commandTexts.Add(AddSubPriceToCommand(sqlCommand, sub));
                }
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async Task<int> CountUnscannedSubsAsync()
        {
            var today = dateTimeProvider.UtcNow.Date;

            using var context = dbContextFactory.CreateDbContext();

            return await context.Subs.CountAsync(x => x.UtcDateTimeLastModified < today);
        }

        public async Task<IEnumerable<long>> GetSubIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection)
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
                .Subs
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

        private static string AddSubDetailsToCommand(DbCommand command, Sub subData)
        {
            var subId = subData.SubId;

            var idParameter = command.CreateParameter();
            var titleParameter = command.CreateParameter();
            var isActiveParameter = command.CreateParameter();

            var idParameterName = $"sub_Id_{subId}";
            var titleParameterName = $"sub_Title_{subId}";
            var isActiveParameterName = $"sub_IsActive_{subId}";

            idParameter.ParameterName = idParameterName;
            idParameter.Value = subId;
            idParameter.DbType = DbType.Int64;
            idParameter.Direction = ParameterDirection.Input;

            titleParameter.ParameterName = titleParameterName;
            titleParameter.Value = subData.Title;
            titleParameter.DbType = DbType.String;
            titleParameter.Direction = ParameterDirection.Input;

            isActiveParameter.ParameterName = isActiveParameterName;
            isActiveParameter.Value = subData.IsActive;
            isActiveParameter.DbType = DbType.Boolean;
            isActiveParameter.Direction = ParameterDirection.Input;

            command.Parameters.Add(idParameter);
            command.Parameters.Add(titleParameter);
            command.Parameters.Add(isActiveParameter);

            return string.Concat(
                $"UPDATE [dbo].[Subs] ",
                $"SET [Title] = @{titleParameterName}, [UtcDateTimeLastModified] = SYSUTCDATETIME(), [IsActive] = @{isActiveParameterName} ",
                $"WHERE [Id] = @{idParameterName}");
        }

        private static string AddSubPriceToCommand(DbCommand command, Sub subData)
        {
            if (subData.Price is null)
            {
                return string.Empty;
            }

            var subId = subData.SubId;

            var idParameter = command.CreateParameter();
            var priceParameter = command.CreateParameter();
            var currencyParameter = command.CreateParameter();

            var idParameterName = $"subPrice_SubId_{subId}";
            var priceParameterName = $"subPrice_Price_{subId}";
            var currencyParameterName = $"subPrice_Currency_{subId}";

            idParameter.ParameterName = idParameterName;
            idParameter.Value = subId;
            idParameter.DbType = DbType.Int64;
            idParameter.Direction = ParameterDirection.Input;

            priceParameter.ParameterName = priceParameterName;
            priceParameter.Value = subData.Price.Value;
            priceParameter.DbType = DbType.Decimal;
            priceParameter.Direction = ParameterDirection.Input;

            currencyParameter.ParameterName = currencyParameterName;
            currencyParameter.Value = subData.Price.Currency;
            currencyParameter.DbType = DbType.String;
            currencyParameter.Direction = ParameterDirection.Input;

            command.Parameters.Add(idParameter);
            command.Parameters.Add(priceParameter);
            command.Parameters.Add(currencyParameter);

            return string.Concat(
                $"INSERT INTO [dbo].[SubPrices] ([SubId], [UtcDateTimeRecorded], [Price], [Currency]) ",
                $"VALUES (@{idParameterName}, SYSUTCDATETIME(), @{priceParameter}, @{currencyParameterName})");
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

        private static async Task<DbCommand> CreateSqlCommandAsync(SteamContext context)
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