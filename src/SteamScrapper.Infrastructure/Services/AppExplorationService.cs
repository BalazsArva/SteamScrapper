using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Infrastructure.Services
{
    public class AppExplorationService : IAppExplorationService
    {
        private readonly SqlConnection sqlConnection;
        private readonly IDatabase redisDatabase;

        public AppExplorationService(IConnectionMultiplexer connectionMultiplexer, SqlConnection sqlConnection)
        {
            if (connectionMultiplexer is null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }

            redisDatabase = connectionMultiplexer.GetDatabase();

            // TODO: Swap to a data access abstraction
            this.sqlConnection = sqlConnection ?? throw new ArgumentNullException(nameof(sqlConnection));
        }

        public async Task<IEnumerable<int>> GetNextAppIdsAsync(DateTime executionDate)
        {
            const int batchSize = 20;

            // TODO: This solution does not support retrying if processing a page dies midway through.
            // Should think about a reserve-process-expire/commit approach.
            var nextPage = await redisDatabase.StringIncrementAsync($"AppExplorer:{executionDate:yyyyMMdd}:Page");

            using var sqlCommand = await CreateSqlCommandAsync();

            sqlCommand.Parameters.AddWithValue("offset", (nextPage - 1) * batchSize);
            sqlCommand.Parameters.AddWithValue("batch_size", batchSize);
            sqlCommand.CommandText = $"SELECT [Id] FROM [SteamScrapper].[dbo].[Apps] ORDER BY [ID] ASC OFFSET @offset ROWS FETCH NEXT @batch_size ROWS ONLY";

            using var sqlDataReader = await sqlCommand.ExecuteReaderAsync();

            var appIds = new List<int>(batchSize);
            while (await sqlDataReader.ReadAsync())
            {
                appIds.Add(sqlDataReader.GetInt32(0));
            }

            if (appIds.Count == 0)
            {
                return Array.Empty<int>();
            }

            var redisTransaction = redisDatabase.CreateTransaction();
            var reservationTasks = new Dictionary<int, Task<bool>>(appIds.Count);

            for (var i = 0; i < appIds.Count; ++i)
            {
                var appId = appIds[i];
                var redisKey = $"AppExplorer:{executionDate:yyyyMMdd}:{appId}";

                reservationTasks[appId] = redisTransaction.StringSetAsync(redisKey, "-", TimeSpan.FromMinutes(5), When.NotExists);
            }

            await redisTransaction.ExecuteAsync();

            var results = new List<int>(appIds.Count);

            foreach (var (appId, couldReserveTask) in reservationTasks)
            {
                if (await couldReserveTask)
                {
                    results.Add(appId);
                }
            }

            return results;
        }

        public async Task UpdateAppsAsync(Dictionary<int, string> idsWithTitles)
        {
            if (idsWithTitles is null)
            {
                throw new ArgumentNullException(nameof(idsWithTitles));
            }

            if (idsWithTitles.Count == 0)
            {
                return;
            }

            using var sqlCommand = await CreateSqlCommandAsync();

            var commandTexts = new List<string>(idsWithTitles.Count);

            foreach (var (appId, appTitle) in idsWithTitles)
            {
                commandTexts.Add(IncludeUpdateAppTitle(sqlCommand, appId, appTitle));
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            await sqlCommand.ExecuteNonQueryAsync();
        }

        private static string IncludeUpdateAppTitle(SqlCommand command, int appId, string title)
        {
            var idParameterName = $"appId_{appId}";
            var titleParameterName = $"appTitle_{appId}";

            command.Parameters.AddWithValue(idParameterName, appId);
            command.Parameters.AddWithValue(titleParameterName, title);

            return $"UPDATE [dbo].[Apps] SET [Title] = @{titleParameterName} WHERE [Id] = @{idParameterName}";
        }

        private async Task<SqlCommand> CreateSqlCommandAsync()
        {
            if (sqlConnection.State == ConnectionState.Closed || sqlConnection.State == ConnectionState.Broken)
            {
                await sqlConnection.OpenAsync();
            }

            return sqlConnection.CreateCommand();
        }
    }
}