using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.SubExplorer.Commands.ProcessSubBatch;

namespace SteamScrapper.SubExplorer.BackgroundServices
{
    public class ProcessSubsBackgroundService : BackgroundService
    {
        private const int DelayMillis = 5000;

        private readonly IProcessSubBatchCommandHandler processSubBatchCommandHandler;
        private readonly ILogger logger;

        public ProcessSubsBackgroundService(
            IProcessSubBatchCommandHandler handler,
            ILogger<ProcessSubsBackgroundService> logger)
        {
            this.processSubBatchCommandHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var shouldDelay = true;

                try
                {
                    var result = await processSubBatchCommandHandler.ProcessSubBatchAsync(stoppingToken);

                    if (result == ProcessSubBatchCommandResult.Success)
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