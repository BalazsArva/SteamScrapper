using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;
using SteamScrapper.Infrastructure.Redis;

namespace SteamScrapper.Infrastructure.Services
{
    public class SubScanningService : ISubScanningService
    {
        private readonly ISubQueryRepository subQueryRepository;
        private readonly SqlConnection sqlConnection;
        private readonly IDatabase redisDatabase;
        private readonly IDateTimeProvider dateTimeProvider;

        public SubScanningService(IRedisConnectionWrapper redisConnectionWrapper, IDateTimeProvider dateTimeProvider, ISubQueryRepository subQueryRepository, SqlConnection sqlConnection)
        {
            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            // TODO: Swap to a data access abstraction
            this.sqlConnection = sqlConnection ?? throw new ArgumentNullException(nameof(sqlConnection));
            this.subQueryRepository = subQueryRepository ?? throw new ArgumentNullException(nameof(subQueryRepository));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<IEnumerable<long>> GetNextSubIdsForScanningAsync(DateTime executionDate)
        {
            const int batchSize = 50;

            var attempt = 1;
            var results = new List<long>(batchSize);

            // Read subIds form the DB that are not scanned today and try to acquire reservation against concurrent processing.
            // If nothing is retrieved from the DB, then we are done for today.
            while (true)
            {
                var subIds = await subQueryRepository.GetSubIdsNotScannedFromAsync(dateTimeProvider.UtcNow.Date, attempt, batchSize, SortDirection.Descending);

                if (!subIds.Any())
                {
                    // Could not find anything in the database waiting for scanning today.
                    return Array.Empty<long>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<long, Task<bool>>(batchSize);

                foreach (var subId in subIds)
                {
                    var redisKey = $"SubScanner:{executionDate:yyyyMMdd}:{subId}";

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