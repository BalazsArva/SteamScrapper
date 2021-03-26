using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Redis;

namespace SteamScrapper.Infrastructure.Services
{
    public class BundleScanningService : IBundleScanningService
    {
        private readonly SqlConnection sqlConnection;
        private readonly IDatabase redisDatabase;

        public BundleScanningService(IRedisConnectionWrapper redisConnectionWrapper, SqlConnection sqlConnection)
        {
            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            // TODO: Swap to a data access abstraction
            this.sqlConnection = sqlConnection ?? throw new ArgumentNullException(nameof(sqlConnection));
        }

        public async Task<IEnumerable<long>> GetNextBundleIdsForScanningAsync(DateTime executionDate)
        {
            const int batchSize = 50;
            const string offsetSqlParamName = "offset";
            const string batchSizeSqlParamName = "batchSize";

            var attempt = 0;
            var bundleIds = new List<long>(batchSize);
            var results = new List<long>(batchSize);

            // Read bundleIds form the DB that are not scanned today and try to acquire reservation against concurrent processing.
            // If nothing is retrieved from the DB, then we are done for today.
            while (true)
            {
                bundleIds.Clear();
                results.Clear();

                using var sqlCommand = await CreateSqlCommandAsync();

                sqlCommand.Parameters.AddWithValue(offsetSqlParamName, attempt * batchSize);
                sqlCommand.Parameters.AddWithValue(batchSizeSqlParamName, batchSize);
                sqlCommand.CommandText =
                    $"SELECT [Id] FROM [SteamScrapper].[dbo].[Bundles] " +
                    $"WHERE [UtcDateTimeLastModified] < CONVERT(date, SYSUTCDATETIME()) " +
                    $"ORDER BY [ID] DESC " +
                    $"OFFSET @{offsetSqlParamName} ROWS " +
                    $"FETCH NEXT @{batchSizeSqlParamName} ROWS ONLY";

                using var sqlDataReader = await sqlCommand.ExecuteReaderAsync();
                while (await sqlDataReader.ReadAsync())
                {
                    bundleIds.Add(sqlDataReader.GetInt64(0));
                }

                if (bundleIds.Count == 0)
                {
                    // Could not find anything in the database waiting for scanning today.
                    return Array.Empty<long>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<long, Task<bool>>(batchSize);

                for (var i = 0; i < bundleIds.Count; ++i)
                {
                    var bundleId = bundleIds[i];
                    var redisKey = $"BundleScanner:{executionDate:yyyyMMdd}:{bundleId}";

                    reservationTasks[bundleId] = reservationTransaction.StringSetAsync(redisKey, string.Empty, TimeSpan.FromMinutes(1), When.NotExists);
                }

                await reservationTransaction.ExecuteAsync();

                foreach (var (bundleId, reserveBundleIdTask) in reservationTasks)
                {
                    if (await reserveBundleIdTask)
                    {
                        // Could reserve bundleId for processing - i.e. no other instance is trying to process this item concurrently.
                        results.Add(bundleId);
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