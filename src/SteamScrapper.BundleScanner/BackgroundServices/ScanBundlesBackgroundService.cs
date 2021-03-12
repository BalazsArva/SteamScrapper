using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.BundleScanner.Commands.ScanBundleBatch;

namespace SteamScrapper.BundleScanner.BackgroundServices
{
    public class ScanBundlesBackgroundService : BackgroundService
    {
        private const int DelayMillis = 5000;

        private readonly IScanBundleBatchCommandHandler handler;
        private readonly ILogger logger;

        public ScanBundlesBackgroundService(
            IScanBundleBatchCommandHandler handler,
            ILogger<ScanBundlesBackgroundService> logger)
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
                    var result = await handler.ScanBundleBatchAsync(stoppingToken);

                    if (result == ScanBundleBatchCommandResult.Success)
                    {
                        shouldDelay = false;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An unhandled error occurred while scanning a batch of bundles.");
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

            logger.LogInformation("Application shutdown requested. The bundle scanning service has been successfully stopped.");
        }
    }
}