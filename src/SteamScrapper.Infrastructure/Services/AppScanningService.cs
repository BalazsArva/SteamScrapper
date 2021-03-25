﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Domain.Services.Abstractions;
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

        public async Task<IEnumerable<long>> GetNextAppIdsForScanningAsync(DateTime executionDate)
        {
            const int batchSize = 50;
            const string offsetSqlParamName = "offset";
            const string batchSizeSqlParamName = "batchSize";

            var attempt = 0;
            var appIds = new List<long>(batchSize);
            var results = new List<long>(batchSize);

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
                    appIds.Add(sqlDataReader.GetInt64(0));
                }

                if (appIds.Count == 0)
                {
                    // Could not find anything in the database waiting for scanning today.
                    return Array.Empty<long>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<long, Task<bool>>(batchSize);

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