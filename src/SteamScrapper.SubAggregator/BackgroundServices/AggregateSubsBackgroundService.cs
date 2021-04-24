using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.SubAggregator.Commands.AggregateSubBatch;

namespace SteamScrapper.SubAggregator.BackgroundServices
{
    public class AggregateSubsBackgroundService : BackgroundService
    {
        private const int DelaySecondsOnUnknownError = 60;
        private const int DelaySecondsOnNoMoreItems = 300;

        private readonly IAggregateSubBatchCommandHandler handler;
        private readonly ILogger logger;

        public AggregateSubsBackgroundService(
            IAggregateSubBatchCommandHandler handler,
            ILogger<AggregateSubsBackgroundService> logger)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delaySeconds = 0;

                try
                {
                    var result = await handler.AggregateSubBatchAsync(stoppingToken);

                    if (result == AggregateSubBatchCommandResult.NoMoreItems)
                    {
                        logger.LogInformation("No more subs were found for aggregating. Retrying in {@Delay} seconds.", DelaySecondsOnNoMoreItems);
                        delaySeconds = DelaySecondsOnNoMoreItems;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An unhandled error occurred while aggregating a batch of subs. Retrying in {@Delay} seconds.", DelaySecondsOnUnknownError);
                    delaySeconds = DelaySecondsOnUnknownError;
                }

                if (delaySeconds > 0)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }
    }
}