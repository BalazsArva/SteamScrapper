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
    public class SubAggregationService : ISubAggregationService
    {
        private readonly IDatabase redisDatabase;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ISubQueryRepository subQueryRepository;
        private readonly ISubWriteRepository subWriteRepository;

        public SubAggregationService(
            IDateTimeProvider dateTimeProvider,
            IRedisConnectionWrapper redisConnectionWrapper,
            ISubQueryRepository subQueryRepository,
            ISubWriteRepository subWriteRepository)
        {
            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            this.subQueryRepository = subQueryRepository ?? throw new ArgumentNullException(nameof(subQueryRepository));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.subWriteRepository = subWriteRepository ?? throw new ArgumentNullException(nameof(subWriteRepository));
        }

        public async Task<int> CountUnaggregatedSubsAsync()
        {
            return await subQueryRepository.CountUnaggregatedSubsFromAsync(dateTimeProvider.UtcNow.Date);
        }

        public async Task ConfirmAggregationAsync(IEnumerable<long> subIds)
        {
            if (subIds is null)
            {
                throw new ArgumentNullException(nameof(subIds));
            }

            if (!subIds.Any())
            {
                return;
            }

            await subWriteRepository.AddSubAggregationsAsync(subIds, dateTimeProvider.UtcNow);
        }

        // TODO: Consolidate date usage (parameter or provider?)
        public async Task<IEnumerable<long>> GetNextSubIdsForAggregationAsync(DateTime executionDate)
        {
            const int batchSize = 50;

            var attempt = 1;
            var results = new List<long>(batchSize);

            // Read subIds form the DB that are not aggregated today and try to acquire reservation against concurrent processing.
            // If nothing is retrieved from the DB, then we are done for today.
            while (true)
            {
                var subIds = await subQueryRepository.GetSubIdsNotAggregatedFromAsync(dateTimeProvider.UtcNow.Date, attempt, batchSize, SortDirection.Descending);

                if (!subIds.Any())
                {
                    // Could not find anything in the database waiting for aggregation today.
                    return Array.Empty<long>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<long, Task<bool>>(batchSize);

                foreach (var subId in subIds)
                {
                    var redisKey = $"SubAggregator:{executionDate:yyyyMMdd}:{subId}";

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