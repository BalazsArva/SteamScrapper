using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.AppScanner.Commands.ScanAppBatch;

namespace SteamScrapper.AppScanner.BackgroundServices
{
    public class ScanAppsBackgroundService : BackgroundService
    {
        private const int DelayMillis = 5000;

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
                var shouldDelay = true;

                try
                {
                    var result = await handler.ScanAppBatchAsync(stoppingToken);

                    if (result == ScanAppBatchCommandResult.Success)
                    {
                        shouldDelay = false;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An unhandled error occurred while scanning a batch of apps.");
                }

                if (shouldDelay)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(DelayMillis), stoppingToken);
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