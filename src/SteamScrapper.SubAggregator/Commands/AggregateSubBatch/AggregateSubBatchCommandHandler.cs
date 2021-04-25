using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.Domain.Models.Aggregates;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.SubAggregator.Services;

namespace SteamScrapper.SubAggregator.Commands.AggregateSubBatch
{
    public class AggregateSubBatchCommandHandler : IAggregateSubBatchCommandHandler
    {
        private readonly ILogger logger;
        private readonly ISubAggregateRepository subAggregateRepository;
        private readonly ISubAggregationService subAggregationService;
        private readonly ISubQueryRepository queryRepository;

        public AggregateSubBatchCommandHandler(
            ILogger<AggregateSubBatchCommandHandler> logger,
            ISubAggregateRepository subAggregateRepository,
            ISubAggregationService subAggregationService,
            ISubQueryRepository queryRepository)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.subAggregateRepository = subAggregateRepository ?? throw new ArgumentNullException(nameof(subAggregateRepository));
            this.subAggregationService = subAggregationService ?? throw new ArgumentNullException(nameof(subAggregationService));
            this.queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
        }

        public async Task<AggregateSubBatchCommandResult> AggregateSubBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            var subIdsToAggregate = await subAggregationService.GetNextSubIdsForAggregationAsync();
            if (!subIdsToAggregate.Any())
            {
                logger.LogInformation("Could not find more subs to aggregate.");
                return AggregateSubBatchCommandResult.NoMoreItems;
            }

            var subAggregates = new List<Sub>();

            foreach (var subId in subIdsToAggregate)
            {
                var doc = await CreateSubAggregateAsync(subId);

                subAggregates.Add(doc);
            }

            await subAggregateRepository.StoreSubAggregatesAsync(subAggregates);
            await subAggregationService.ConfirmAggregationAsync(subIdsToAggregate);

            var remainingCount = await subAggregationService.CountUnaggregatedSubsAsync();

            stopwatch.Stop();

            logger.LogInformation(
                "Aggregated {@SubIdsCount} subs in {@ElapsedMillis} millis. About {@RemainingCount} subs still need to be aggregated.",
                subIdsToAggregate.Count(),
                stopwatch.ElapsedMilliseconds,
                remainingCount);

            return AggregateSubBatchCommandResult.Success;
        }

        private async Task<Sub> CreateSubAggregateAsync(long subId)
        {
            var sub = await queryRepository.GetSubBasicDetailsByIdAsync(subId);
            var subPriceHistory = await queryRepository.GetSubPriceHistoryByIdAsync(subId);

            var result = new Sub
            {
                Title = sub.Title,
                Id = sub.SubId.ToString(CultureInfo.InvariantCulture),
                IsActive = sub.IsActive,
            };

            var priceHistoriesByCurrency = subPriceHistory.GroupBy(x => x.Currency);

            foreach (var priceHistory in priceHistoriesByCurrency)
            {
                var currencySymbol = priceHistory.Key;
                var currencyName = currencySymbol == "€" ? "EUR" : null;

                if (currencyName is null)
                {
                    logger.LogWarning("Could not resolve currency name for currency symbol '{@CurrencySymbol} for sub {@SubId}.", currencySymbol, subId);
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