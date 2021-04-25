using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Infrastructure.Redis;

namespace SteamScrapper.AppScanner.Services
{
    public class AppScanningService : IAppScanningService
    {
        private readonly IAppQueryRepository appQueryRepository;
        private readonly IDatabase redisDatabase;
        private readonly IDateTimeProvider dateTimeProvider;

        public AppScanningService(IRedisConnectionWrapper redisConnectionWrapper, IDateTimeProvider dateTimeProvider, IAppQueryRepository appQueryRepository)
        {
            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            this.appQueryRepository = appQueryRepository ?? throw new ArgumentNullException(nameof(appQueryRepository));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<int> CountUnscannedAppsAsync()
        {
            return await appQueryRepository.CountUnscannedAppsAsync(dateTimeProvider.UtcNow.Date);
        }

        public async Task<IEnumerable<long>> GetNextAppIdsForScanningAsync()
        {
            const int batchSize = 50;

            var attempt = 1;
            var results = new List<long>(batchSize);
            var utcDate = dateTimeProvider.UtcNow.Date;

            // Read appIds form the DB that are not scanned today and try to acquire reservation against concurrent processing.
            // If nothing is retrieved from the DB, then we are done for today.
            while (true)
            {
                var appIds = await appQueryRepository.GetAppIdsNotScannedFromAsync(utcDate, attempt, batchSize, SortDirection.Descending);

                if (!appIds.Any())
                {
                    // Could not find anything in the database waiting for scanning today.
                    return Array.Empty<long>();
                }

                var reservationTransaction = redisDatabase.CreateTransaction();
                var reservationTasks = new Dictionary<long, Task<bool>>(batchSize);

                foreach (var appId in appIds)
                {
                    var redisKey = $"AppScanner:{utcDate:yyyyMMdd}:{appId}";

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
    }
}