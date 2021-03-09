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
    public class SubExplorationService : ISubScanningService
    {
        private readonly SqlConnection sqlConnection;
        private readonly IDatabase redisDatabase;

        public SubExplorationService(IRedisConnectionWrapper redisConnectionWrapper, SqlConnection sqlConnection)
        {
            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            // TODO: Swap to a data access abstraction
            this.sqlConnection = sqlConnection ?? throw new ArgumentNullException(nameof(sqlConnection));
        }

        public async Task<IEnumerable<int>> GetNextSubIdsForScanningAsync(DateTime executionDate)
        {
            const int batchSize = 50;
            const string offsetSqlParamName = "offset";
            const string batchSizeSqlParamName = "batchSize";

            var attempt = 0;
            var subIds = new List<int>(batchSize);
            var results = new List<int>(batchSize);

            // Read subIds form the DB that are not explored today and try to acquire reservation against concurrent processing.
            // If nothing is retrieved from the DB, then we are done for today.
            while (true)
            {
                subIds.Clear();
                results.Clear();

                using var sqlCommand = await CreateSqlCommandAsync();

                sqlCommand.Parameters.AddWithValue(offsetSqlParamName, attempt * batchSize);
                sqlCommand.Parameters.AddWithValue(batchSizeSqlParamName, batchSize);
                sqlCommand.CommandText =
                    $"SELECT [Id] FROM [SteamScrapper].[dbo].[Subs] " +
                    $"WHERE [UtcDateTimeLastModified] < CONVERT(date, SYSUTCDATETIME()) " +
                    $"ORDER BY [ID] DESC " +
                    $"OFFSET @{offsetSqlParamName} ROWS " +
                    $"FETCH NEXT @{batchSizeSqlParamName} ROWS ONLY";

                using var sqlDataReader = await sqlCommand.ExecuteReaderAsync();
                while (await sqlDataReader.ReadAsync())
                {
                    subIds.Add(sqlDataReader.GetInt32(0));
                }

                if (subIds.Count == 0)
                {
                    // Could not find anything in the database waiting for exploration today.
                    return Array.Empty<int>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<int, Task<bool>>(batchSize);

                for (var i = 0; i < subIds.Count; ++i)
                {
                    var subId = subIds[i];
                    var redisKey = $"SubExplorer:{executionDate:yyyyMMdd}:{subId}";

                    reservationTasks[subId] = reservationTransaction.StringSetAsync(redisKey, string.Empty, TimeSpan.FromMinutes(1), When.NotExists);
                }

                await reservationTransaction.ExecuteAsync();

                foreach (var (subId, reserveSubIdTask) in reservationTasks)
                {
                    if (await reserveSubIdTask)
                    {
                        // Could reserve subId for processing - i.e. no other instance is trying to process this item concurrently.
                        results.Add(subId);
                    }
                }

                if (results.Count > 0)
                {
                    return results;
                }

                ++attempt;
            }
        }

        public async Task UpdateSubsAsync(IEnumerable<SubData> subData)
        {
            if (subData is null)
            {
                throw new ArgumentNullException(nameof(subData));
            }

            if (!subData.Any())
            {
                return;
            }

            using var sqlCommand = await CreateSqlCommandAsync();

            var commandTexts = new List<string>();

            foreach (var sub in subData)
            {
                commandTexts.Add(AddSubDetailsToUpdateCommand(sqlCommand, sub));
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            await sqlCommand.ExecuteNonQueryAsync();
        }

        private static string AddSubDetailsToUpdateCommand(SqlCommand command, SubData subData)
        {
            var subId = subData.SubId;

            var idParameterName = $"subId_{subId}";
            var titleParameterName = $"subTitle_{subId}";

            command.Parameters.AddWithValue(idParameterName, subId);
            command.Parameters.AddWithValue(titleParameterName, subData.Title);

            return string.Concat(
                $"UPDATE [dbo].[Subs] ",
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