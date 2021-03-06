using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.AppExplorer.Commands.ProcessAppBatch;

namespace SteamScrapper.AppExplorer.BackgroundServices
{
    public class ProcessAppsBackgroundService : BackgroundService
    {
        private const int DelayMillis = 5000;

        private readonly IProcessAppBatchCommandHandler processAppBatchCommandHandler;
        private readonly ILogger<ProcessAppsBackgroundService> logger;

        public ProcessAppsBackgroundService(
            IProcessAppBatchCommandHandler handler,
            ILogger<ProcessAppsBackgroundService> logger)
        {
            this.processAppBatchCommandHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var shouldDelay = true;

                try
                {
                    var result = await processAppBatchCommandHandler.ProcessAppBatchAsync(stoppingToken);

                    if (result == ProcessAppBatchCommandResult.Success)
                    {
                        shouldDelay = false;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An unhandled error occurred while processing a batch of subs.");
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

            logger.LogInformation("Application shutdown requested. The sub exploration service has been successfully stopped.");
        }
    }
}