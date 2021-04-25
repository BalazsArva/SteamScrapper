using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Redis;

namespace SteamScrapper.Infrastructure.Services
{
    public class SubScanningService : ISubScanningService
    {
        private readonly ISubQueryRepository subQueryRepository;
        private readonly IDatabase redisDatabase;
        private readonly IDateTimeProvider dateTimeProvider;

        public SubScanningService(IRedisConnectionWrapper redisConnectionWrapper, IDateTimeProvider dateTimeProvider, ISubQueryRepository subQueryRepository)
        {
            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            this.subQueryRepository = subQueryRepository ?? throw new ArgumentNullException(nameof(subQueryRepository));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<int> CountUnscannedSubsAsync()
        {
            return await subQueryRepository.CountUnscannedSubsFromAsync(dateTimeProvider.UtcNow.Date);
        }

        public async Task<IEnumerable<long>> GetNextSubIdsForScanningAsync()
        {
            const int batchSize = 50;

            var attempt = 1;
            var results = new List<long>(batchSize);
            var utcDate = dateTimeProvider.UtcNow.Date;

            // Read subIds form the DB that are not scanned today and try to acquire reservation against concurrent processing.
            // If nothing is retrieved from the DB, then we are done for today.
            while (true)
            {
                var subIds = await subQueryRepository.GetSubIdsNotScannedFromAsync(utcDate, attempt, batchSize, SortDirection.Descending);

                if (!subIds.Any())
                {
                    // Could not find anything in the database waiting for scanning today.
                    return Array.Empty<long>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<long, Task<bool>>(batchSize);

                foreach (var subId in subIds)
                {
                    var redisKey = $"SubScanner:{utcDate:yyyyMMdd}:{subId}";

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
    }
}