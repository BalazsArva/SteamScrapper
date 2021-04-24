using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.RavenDb;

namespace SteamScrapper.SubAggregator.Commands.AggregateSubBatch
{
    public class AggregateSubBatchCommandHandler : IAggregateSubBatchCommandHandler
    {
        private readonly ILogger logger;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IDocumentStoreWrapper documentStoreWrapper;
        private readonly ISubAggregationService subAggregationService;
        private readonly ISubQueryRepository queryRepository;

        public AggregateSubBatchCommandHandler(
            ILogger<AggregateSubBatchCommandHandler> logger,
            IDateTimeProvider dateTimeProvider,
            IDocumentStoreWrapper documentStoreWrapper,
            ISubAggregationService subAggregationService,
            ISubQueryRepository queryRepository)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.documentStoreWrapper = documentStoreWrapper ?? throw new ArgumentNullException(nameof(documentStoreWrapper));
            this.subAggregationService = subAggregationService ?? throw new ArgumentNullException(nameof(subAggregationService));
            this.queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
        }

        public async Task<AggregateSubBatchCommandResult> AggregateSubBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var utcNow = dateTimeProvider.UtcNow;

            var subIdsToAggregate = await subAggregationService.GetNextSubIdsForAggregationAsync(utcNow);
            if (!subIdsToAggregate.Any())
            {
                logger.LogInformation("Could not find more subs to aggregate.");
                return AggregateSubBatchCommandResult.NoMoreItems;
            }

            // TODO: Use a repo
            using var session = documentStoreWrapper.DocumentStore.OpenAsyncSession();

            foreach (var subId in subIdsToAggregate)
            {
                var sub = await queryRepository.GetSubBasicDetailsByIdAsync(subId);
                var subPriceHistory = await queryRepository.GetSubPriceHistoryByIdAsync(subId);

                var doc = new SubDocument
                {
                    Title = sub.Title,
                    Id = sub.SubId.ToString(CultureInfo.InvariantCulture),
                };

                var priceHistoriesByCurrency = subPriceHistory.GroupBy(x => x.Currency);

                foreach (var priceHistoryByCurrency in priceHistoriesByCurrency)
                {
                    var currency = priceHistoryByCurrency.Key;

                    // TODO: Sort by date (field is missing)
                    // priceHistoryByCurrency.OrderBy(x => x.utc)

                    var temp = new List<SubPriceHistoryEntry>();
                    doc.PriceHistoryByCurrency[currency] = temp;

                    /*
                    foreach (var price in priceHistoryByCurrency)
                    {
                        var prev = temp.LastOrDefault();
                        if (prev is null || prev.DiscountPrice != price.DiscountValue || prev.NormalPrice != price.Value)
                        {
                            // Price is changed, add new entry.
                            temp.Add(new SubPriceHistoryEntry
                            {
                                NormalPrice = price.Value,
                                DiscountPrice = price.DiscountValue,
                                // UtcDateTimeRecorded = price.
                            })
                        }
                    }
                    */
                }

                await session.StoreAsync(doc, subId.ToString(CultureInfo.InvariantCulture));
            }

            await session.SaveChangesAsync();
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

        private class SubDocument
        {
            public string Id { get; set; }

            public string Title { get; set; }

            public Dictionary<string, List<SubPriceHistoryEntry>> PriceHistoryByCurrency { get; } = new Dictionary<string, List<SubPriceHistoryEntry>>();
        }

        private class SubPriceHistoryEntry
        {
            public decimal NormalPrice { get; set; }

            public decimal? DiscountPrice { get; set; }

            public DateTime UtcDateTimeRecorded { get; set; }
        }
    }
}