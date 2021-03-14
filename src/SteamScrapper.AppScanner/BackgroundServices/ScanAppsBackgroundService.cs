using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.AppScanner.Commands.ScanAppBatch;
using SteamScrapper.Domain.Services.Exceptions;

namespace SteamScrapper.AppScanner.BackgroundServices
{
    public class ScanAppsBackgroundService : BackgroundService
    {
        private const int DelaySecondsOnUnknownError = 60;
        private const int DelaySecondsOnRateLimitExceededError = 300;
        private const int DelaySecondsOnNoMoreItems = 300;

        private readonly IScanAppBatchCommandHandler handler;
        private readonly ILogger<ScanAppsBackgroundService> logger;

        public ScanAppsBackgroundService(
            IScanAppBatchCommandHandler handler,
            ILogger<ScanAppsBackgroundService> logger)
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
                    var result = await handler.ScanAppBatchAsync(stoppingToken);

                    if (result == ScanAppBatchCommandResult.NoMoreItems)
                    {
                        logger.LogInformation("No more apps were found for scanning. Retrying in {@Delay} seconds.", DelaySecondsOnNoMoreItems);
                        delaySeconds = DelaySecondsOnNoMoreItems;
                    }
                }
                catch (SteamRateLimitExceededException e)
                {
                    logger.LogError(e, "The request rate limit has been exceeded while scanning a batch of apps. Retrying in {@Delay} seconds.", DelaySecondsOnRateLimitExceededError);
                    delaySeconds = DelaySecondsOnRateLimitExceededError;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An unhandled error occurred while scanning a batch of apps. Retrying in {@Delay} seconds.", DelaySecondsOnUnknownError);
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

            logger.LogInformation("Application shutdown requested. The app scanning service has been successfully stopped.");
        }
    }
}