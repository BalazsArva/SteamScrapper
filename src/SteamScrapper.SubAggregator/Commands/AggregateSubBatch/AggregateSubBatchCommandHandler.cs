using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.SubAggregator.Commands.AggregateSubBatch
{
    public class AggregateSubBatchCommandHandler : IAggregateSubBatchCommandHandler
    {
        private readonly ILogger logger;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ISubAggregationService subAggregationService;

        public AggregateSubBatchCommandHandler(
            ILogger<AggregateSubBatchCommandHandler> logger,
            IDateTimeProvider dateTimeProvider,
            ISubAggregationService subAggregationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.subAggregationService = subAggregationService ?? throw new ArgumentNullException(nameof(subAggregationService));
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

            var remainingCount = await subAggregationService.CountUnscannedSubsAsync();

            stopwatch.Stop();

            logger.LogInformation(
                "Aggregated {@SubIdsCount} subs in {@ElapsedMillis} millis. About {@RemainingCount} subs still need to be aggregated.",
                subIdsToAggregate.Count(),
                stopwatch.ElapsedMilliseconds,
                remainingCount);

            return AggregateSubBatchCommandResult.Success;
        }
    }
}