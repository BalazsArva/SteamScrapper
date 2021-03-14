using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.SubScanner.Commands.ScanSubBatch;

namespace SteamScrapper.SubScanner.BackgroundServices
{
    public class ScanSubsBackgroundService : BackgroundService
    {
        private const int DelaySecondsOnError = 60;
        private const int DelaySecondsOnNoMoreItems = 300;

        private readonly IScanSubBatchCommandHandler handler;
        private readonly ILogger logger;

        public ScanSubsBackgroundService(
            IScanSubBatchCommandHandler handler,
            ILogger<ScanSubsBackgroundService> logger)
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
                    var result = await handler.ScanSubBatchAsync(stoppingToken);

                    if (result == ScanSubBatchCommandResult.NoMoreItems)
                    {
                        logger.LogInformation("No more subs were found for scanning. Retrying in {@Delay} seconds.", DelaySecondsOnNoMoreItems);
                        delaySeconds = DelaySecondsOnNoMoreItems;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An unhandled error occurred while scanning a batch of subs. Retrying in {@Delay} seconds.", DelaySecondsOnError);

                    delaySeconds = DelaySecondsOnError;
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

            logger.LogInformation("Application shutdown requested. The sub scanning service has been successfully stopped.");
        }
    }
}