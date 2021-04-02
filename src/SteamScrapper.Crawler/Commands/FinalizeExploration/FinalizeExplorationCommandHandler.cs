using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Crawler.Commands.FinalizeExploration
{
    public class FinalizeExplorationCommandHandler : IFinalizeExplorationCommandHandler
    {
        private readonly ICrawlerAddressRegistrationService crawlerAddressRegistrationService;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILogger logger;

        public FinalizeExplorationCommandHandler(
            ILogger<FinalizeExplorationCommandHandler> logger,
            IDateTimeProvider dateTimeProvider,
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.crawlerAddressRegistrationService = crawlerAddressRegistrationService ?? throw new ArgumentNullException(nameof(crawlerAddressRegistrationService));
        }

        public async Task FinalizeExplorationAsync()
        {
            logger.LogInformation("Setting expiration on exploration registry...");

            await crawlerAddressRegistrationService.FinalizeExplorationForDateAsync(dateTimeProvider.UtcNow);
        }
    }
}