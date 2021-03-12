using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;
using SteamScrapper.Infrastructure.Redis;

namespace SteamScrapper.Infrastructure.Services
{
    public class AppScanningService : IAppScanningService
    {
        private readonly SqlConnection sqlConnection;
        private readonly IDatabase redisDatabase;

        public AppScanningService(IRedisConnectionWrapper redisConnectionWrapper, SqlConnection sqlConnection)
        {
            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            // TODO: Swap to a data access abstraction
            this.sqlConnection = sqlConnection ?? throw new ArgumentNullException(nameof(sqlConnection));
        }

        public async Task<IEnumerable<int>> GetNextAppIdsForScanningAsync(DateTime executionDate)
        {
            const int batchSize = 50;
            const string offsetSqlParamName = "offset";
            const string batchSizeSqlParamName = "batchSize";

            var attempt = 0;
            var appIds = new List<int>(batchSize);
            var results = new List<int>(batchSize);

            // Read appIds form the DB that are not scanned today and try to acquire reservation against concurrent processing.
            // If nothing is retrieved from the DB, then we are done for today.
            while (true)
            {
                appIds.Clear();
                results.Clear();

                using var sqlCommand = await CreateSqlCommandAsync();

                sqlCommand.Parameters.AddWithValue(offsetSqlParamName, attempt * batchSize);
                sqlCommand.Parameters.AddWithValue(batchSizeSqlParamName, batchSize);
                sqlCommand.CommandText =
                    $"SELECT [Id] FROM [SteamScrapper].[dbo].[Apps] " +
                    $"WHERE [UtcDateTimeLastModified] < CONVERT(date, SYSUTCDATETIME()) " +
                    $"ORDER BY [ID] DESC " +
                    $"OFFSET @{offsetSqlParamName} ROWS " +
                    $"FETCH NEXT @{batchSizeSqlParamName} ROWS ONLY";

                using var sqlDataReader = await sqlCommand.ExecuteReaderAsync();
                while (await sqlDataReader.ReadAsync())
                {
                    appIds.Add(sqlDataReader.GetInt32(0));
                }

                if (appIds.Count == 0)
                {
                    // Could not find anything in the database waiting for scanning today.
                    return Array.Empty<int>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<int, Task<bool>>(batchSize);

                for (var i = 0; i < appIds.Count; ++i)
                {
                    var appId = appIds[i];
                    var redisKey = $"AppScanner:{executionDate:yyyyMMdd}:{appId}";

                    reservationTasks[appId] = reservationTransaction.StringSetAsync(redisKey, string.Empty, TimeSpan.FromMinutes(1), When.NotExists);
                }

                await reservationTransaction.ExecuteAsync();

                foreach (var (appId, reserveAppIdTask) in reservationTasks)
                {
                    if (await reserveAppIdTask)
                    {
                        // Could reserve appId for processing - i.e. no other instance is trying to process this item concurrently.
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
                commandTexts.Add(AddAppDetailsToUpdateCommand(sqlCommand, app));
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            await sqlCommand.ExecuteNonQueryAsync();
        }

        private static string AddAppDetailsToUpdateCommand(SqlCommand command, AppData appData)
        {
            var appId = appData.AppId;

            var idParameterName = $"appId_{appId}";
            var titleParameterName = $"appTitle_{appId}";
            var bannerUrlParameterName = $"appBanner_{appId}";

            command.Parameters.AddWithValue(idParameterName, appId);
            command.Parameters.AddWithValue(titleParameterName, appData.Title);
            command.Parameters.AddWithValue(bannerUrlParameterName, appData.BannerUrl ?? (object)DBNull.Value);

            return string.Concat(
                $"UPDATE [dbo].[Apps] ",
                $"SET [Title] = @{titleParameterName}, [BannerUrl] = @{bannerUrlParameterName}, [UtcDateTimeLastModified] = SYSUTCDATETIME() ",
                $"WHERE [Id] = @{idParameterName}");
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