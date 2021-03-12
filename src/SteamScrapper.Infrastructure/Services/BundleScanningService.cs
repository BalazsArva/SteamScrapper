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

        public async Task<IEnumerable<int>> GetNextBundleIdsForScanningAsync(DateTime executionDate)
        {
            const int batchSize = 50;
            const string offsetSqlParamName = "offset";
            const string batchSizeSqlParamName = "batchSize";

            var attempt = 0;
            var bundleIds = new List<int>(batchSize);
            var results = new List<int>(batchSize);

            // Read bundleIds form the DB that are not explored today and try to acquire reservation against concurrent processing.
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
                    bundleIds.Add(sqlDataReader.GetInt32(0));
                }

                if (bundleIds.Count == 0)
                {
                    // Could not find anything in the database waiting for exploration today.
                    return Array.Empty<int>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<int, Task<bool>>(batchSize);

                for (var i = 0; i < bundleIds.Count; ++i)
                {
                    var bundleId = bundleIds[i];
                    var redisKey = $"BundleExplorer:{executionDate:yyyyMMdd}:{bundleId}";

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

        public async Task UpdateBundlesAsync(IEnumerable<BundleData> bundleData)
        {
            if (bundleData is null)
            {
                throw new ArgumentNullException(nameof(bundleData));
            }

            if (!bundleData.Any())
            {
                return;
            }

            using var sqlCommand = await CreateSqlCommandAsync();

            var commandTexts = new List<string>();

            foreach (var bundle in bundleData)
            {
                commandTexts.Add(AddBundleDetailsToUpdateCommand(sqlCommand, bundle));
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            await sqlCommand.ExecuteNonQueryAsync();
        }

        private static string AddBundleDetailsToUpdateCommand(SqlCommand command, BundleData bundleData)
        {
            var bundleId = bundleData.BundleId;

            var idParameterName = $"bundleId_{bundleId}";
            var titleParameterName = $"bundleTitle_{bundleId}";

            command.Parameters.AddWithValue(idParameterName, bundleId);
            command.Parameters.AddWithValue(titleParameterName, bundleData.Title);

            return string.Concat(
                $"UPDATE [dbo].[Bundles] ",
                $"SET [Title] = @{titleParameterName}, [UtcDateTimeLastModified] = SYSUTCDATETIME() ",
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