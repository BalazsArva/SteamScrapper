using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.BundleAggregator.Services;
using SteamScrapper.Domain.Models.Aggregates;
using SteamScrapper.Domain.Repositories;

namespace SteamScrapper.BundleAggregator.Commands.AggregateBundleBatch
{
    public class AggregateBundleBatchCommandHandler : IAggregateBundleBatchCommandHandler
    {
        private readonly ILogger logger;
        private readonly IBundleAggregateRepository bundleAggregateRepository;
        private readonly IBundleAggregationService bundleAggregationService;
        private readonly IBundleQueryRepository queryRepository;

        public AggregateBundleBatchCommandHandler(
            ILogger<AggregateBundleBatchCommandHandler> logger,
            IBundleAggregateRepository bundleAggregateRepository,
            IBundleAggregationService bundleAggregationService,
            IBundleQueryRepository queryRepository)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.bundleAggregateRepository = bundleAggregateRepository ?? throw new ArgumentNullException(nameof(bundleAggregateRepository));
            this.bundleAggregationService = bundleAggregationService ?? throw new ArgumentNullException(nameof(bundleAggregationService));
            this.queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
        }

        public async Task<AggregateBundleBatchCommandResult> AggregateBundleBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            var bundleIdsToAggregate = await bundleAggregationService.GetNextBundleIdsForAggregationAsync();
            if (!bundleIdsToAggregate.Any())
            {
                logger.LogInformation("Could not find more bundles to aggregate.");
                return AggregateBundleBatchCommandResult.NoMoreItems;
            }

            var bundleAggregates = new List<Bundle>();

            foreach (var bundleId in bundleIdsToAggregate)
            {
                var doc = await CreateBundleAggregateAsync(bundleId);

                bundleAggregates.Add(doc);
            }

            await bundleAggregateRepository.StoreBundleAggregatesAsync(bundleAggregates);
            await bundleAggregationService.ConfirmAggregationAsync(bundleIdsToAggregate);

            var remainingCount = await bundleAggregationService.CountUnaggregatedBundlesAsync();

            stopwatch.Stop();

            logger.LogInformation(
                "Aggregated {@BundleIdsCount} bundles in {@ElapsedMillis} millis. About {@RemainingCount} bundles still need to be aggregated.",
                bundleIdsToAggregate.Count(),
                stopwatch.ElapsedMilliseconds,
                remainingCount);

            return AggregateBundleBatchCommandResult.Success;
        }

        private async Task<Bundle> CreateBundleAggregateAsync(long bundleId)
        {
            var bundle = await queryRepository.GetBundleBasicDetailsByIdAsync(bundleId);
            var bundlePriceHistory = await queryRepository.GetBundlePriceHistoryByIdAsync(bundleId);

            var result = new Bundle
            {
                Title = bundle.Title,
                Id = bundle.BundleId.ToString(CultureInfo.InvariantCulture),
                IsActive = bundle.IsActive,
            };

            var priceHistoriesByCurrency = bundlePriceHistory.GroupBy(x => x.Currency);

            foreach (var priceHistory in priceHistoriesByCurrency)
            {
                var currencySymbol = priceHistory.Key;
                var currencyName = currencySymbol == "€" ? "EUR" : null;

                if (currencyName is null)
                {
                    logger.LogWarning("Could not resolve currency name for currency symbol '{@CurrencySymbol} for bundle {@BundleId}.", currencySymbol, bundleId);
                    continue;
                }

                var priceHistoryForCurrency = new PriceHistory
                {
                    CurrencyName = currencyName,
                    CurrencySymbol = currencySymbol,
                };

                priceHistoryForCurrency.HistoryEntries.AddRange(GetPriceHistoryByCurrency(priceHistory));

                result.PriceHistoryByCurrency.Add(priceHistoryForCurrency);
            }

            return result;
        }

        private static IEnumerable<PriceHistoryEntry> GetPriceHistoryByCurrency(IEnumerable<Domain.Repositories.Models.Price> priceHistoryByCurrency)
        {
            var result = new List<PriceHistoryEntry>();

            foreach (var price in priceHistoryByCurrency.OrderBy(x => x.UtcDateTimeRecorded))
            {
                var prev = result.LastOrDefault();

                // Don't include those price records that contain the same price as the previous.
                if (prev is null || prev.DiscountPrice != price.DiscountValue || prev.NormalPrice != price.Value)
                {
                    result.Add(new PriceHistoryEntry
                    {
                        NormalPrice = price.Value,
                        DiscountPrice = price.DiscountValue,
                        UtcDateTimeRecorded = price.UtcDateTimeRecorded,
                    });
                }
            }

            return result;
        }
    }
}