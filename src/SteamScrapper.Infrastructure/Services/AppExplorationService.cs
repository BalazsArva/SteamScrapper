using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;

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
            const int batchSize = 50;

            var attempt = 0;

            while (true)
            {
                using var sqlCommand = await CreateSqlCommandAsync();

                // TODO: Add indexes for date filtering
                sqlCommand.Parameters.AddWithValue("offset", attempt * batchSize);
                sqlCommand.Parameters.AddWithValue("batch_size", batchSize);
                sqlCommand.CommandText =
                    "SELECT [Id] FROM [SteamScrapper].[dbo].[Apps] " +
                    "WHERE [UtcDateTimeLastModified] < CONVERT(date, SYSUTCDATETIME()) " +
                    "ORDER BY [ID] DESC " +
                    "OFFSET @offset ROWS " +
                    "FETCH NEXT @batch_size ROWS ONLY";

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

                    reservationTasks[appId] = redisTransaction.StringSetAsync(redisKey, "-", TimeSpan.FromMinutes(1), When.NotExists);
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

                if (results.Count > 0)
                {
                    return results;
                }

                ++attempt;
            }
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

            using var sqlCommand = await CreateSqlCommandAsync();

            var commandTexts = new List<string>();

            foreach (var app in appData)
            {
                commandTexts.Add(IncludeUpdateAppDetails(sqlCommand, app));
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            await sqlCommand.ExecuteNonQueryAsync();
        }

        private static string IncludeUpdateAppDetails(SqlCommand command, AppData appData)
        {
            var appId = appData.AppId;

            var idParameterName = $"appId_{appId}";
            var titleParameterName = $"appTitle_{appId}";
            var bannerUrlParameterName = $"appBanner_{appId}";

            command.Parameters.AddWithValue(idParameterName, appId);
            command.Parameters.AddWithValue(titleParameterName, appData.Title);
            command.Parameters.AddWithValue(bannerUrlParameterName, appData.BannerUrl ?? (object)DBNull.Value);

            return
                $"UPDATE [dbo].[Apps] " +
                $"SET [Title] = @{titleParameterName}, [BannerUrl] = @{bannerUrlParameterName}, [UtcDateTimeLastModified] = SYSUTCDATETIME() " +
                $"WHERE [Id] = @{idParameterName}";
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