using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Infrastructure.Redis;

namespace SteamScrapper.BundleScanner.Services
{
    public class BundleScanningService : IBundleScanningService
    {
        private readonly IBundleQueryRepository bundleQueryRepository;
        private readonly IDatabase redisDatabase;
        private readonly IDateTimeProvider dateTimeProvider;

        public BundleScanningService(IRedisConnectionWrapper redisConnectionWrapper, IDateTimeProvider dateTimeProvider, IBundleQueryRepository bundleQueryRepository)
        {
            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            this.bundleQueryRepository = bundleQueryRepository ?? throw new ArgumentNullException(nameof(bundleQueryRepository));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<int> CountUnscannedBundlesAsync()
        {
            return await bundleQueryRepository.CountUnscannedBundlesFromAsync(dateTimeProvider.UtcNow.Date);
        }

        public async Task<IEnumerable<long>> GetNextBundleIdsForScanningAsync()
        {
            const int batchSize = 50;

            var attempt = 1;
            var results = new List<long>(batchSize);
            var utcDate = dateTimeProvider.UtcNow.Date;

            // Read bundleIds form the DB that are not scanned today and try to acquire reservation against concurrent processing.
            // If nothing is retrieved from the DB, then we are done for today.
            while (true)
            {
                var bundleIds = await bundleQueryRepository.GetBundleIdsNotScannedFromAsync(utcDate, attempt, batchSize, SortDirection.Descending);

                if (!bundleIds.Any())
                {
                    // Could not find anything in the database waiting for scanning today.
                    return Array.Empty<long>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<long, Task<bool>>(batchSize);

                foreach (var bundleId in bundleIds)
                {
                    var redisKey = $"BundleScanner:{utcDate:yyyyMMdd}:{bundleId}";

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
    }
}