using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.Crawler.Commands.ExplorePage;
using SteamScrapper.Crawler.Commands.RegisterStartingAddresses;

namespace SteamScrapper.Crawler.BackgroundServices
{
    public class CrawlerBackgroundService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IExplorePageCommandHandler explorePageCommandHandler;
        private readonly IRegisterStartingAddressesCommandHandler registerStartingAddressesCommandHandler;

        public CrawlerBackgroundService(
            ILogger<CrawlerBackgroundService> logger,
            IExplorePageCommandHandler explorePageCommandHandler,
            IRegisterStartingAddressesCommandHandler registerStartingAddressesCommandHandler)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.explorePageCommandHandler = explorePageCommandHandler ?? throw new ArgumentNullException(nameof(explorePageCommandHandler));
            this.registerStartingAddressesCommandHandler = registerStartingAddressesCommandHandler ?? throw new ArgumentNullException(nameof(registerStartingAddressesCommandHandler));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await registerStartingAddressesCommandHandler.RegisterStartingAddressesAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await explorePageCommandHandler.ExplorePageAsync(stoppingToken);
                    if (result == ExplorePageCommandResult.NoMoreItems)
                    {
                        // No links remain to explore.
                        // TODO: Restart the next day. But check cancellation token as well.
                        break;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An unhandled error occurred during the crawling process.");
                }
            }

            logger.LogInformation("Finished crawling.");
        }
    }
}