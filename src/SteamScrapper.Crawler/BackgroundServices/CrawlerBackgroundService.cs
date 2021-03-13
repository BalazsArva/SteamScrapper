using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Providers;
using SteamScrapper.Crawler.Commands.ExplorePage;
using SteamScrapper.Crawler.Commands.RegisterStartingAddresses;

namespace SteamScrapper.Crawler.BackgroundServices
{
    public class CrawlerBackgroundService : BackgroundService
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IExplorePageCommandHandler explorePageCommandHandler;
        private readonly ILogger logger;
        private readonly IRegisterStartingAddressesCommandHandler registerStartingAddressesCommandHandler;

        public CrawlerBackgroundService(
            IDateTimeProvider dateTimeProvider,
            IExplorePageCommandHandler explorePageCommandHandler,
            ILogger<CrawlerBackgroundService> logger,
            IRegisterStartingAddressesCommandHandler registerStartingAddressesCommandHandler)
        {
            this.dateTimeProvider = dateTimeProvider;
            this.explorePageCommandHandler = explorePageCommandHandler ?? throw new ArgumentNullException(nameof(explorePageCommandHandler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.registerStartingAddressesCommandHandler = registerStartingAddressesCommandHandler ?? throw new ArgumentNullException(nameof(registerStartingAddressesCommandHandler));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await registerStartingAddressesCommandHandler.RegisterStartingAddressesAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await explorePageCommandHandler.ExplorePageAsync(stoppingToken);
                        if (result == ExplorePageCommandResult.NoMoreItems)
                        {
                            logger.LogInformation("The crawler process has finished processing all links.");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "An unhandled error occurred during the crawling process.");
                    }
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                // Add 5 minutes to ensure that minor time imprecisions won't cause the process to continue on the same day.
                var tomorrowMorning = dateTimeProvider.UtcNow.Date.AddDays(1).AddMinutes(5);
                var waitTime = tomorrowMorning - dateTimeProvider.UtcNow;

                try
                {
                    await Task.Delay(waitTime, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            logger.LogInformation("Appplication shutdown requested, the crawler service has been terminated.");
        }
    }
}