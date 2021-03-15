using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Crawler.Commands.CancelReservations
{
    public class CancelReservationsCommandHandler : ICancelReservationsCommandHandler
    {
        private readonly ILogger logger;
        private readonly ICrawlerPrefetchService crawlerPrefetchService;
        private readonly IDateTimeProvider dateTimeProvider;

        public CancelReservationsCommandHandler(
            ILogger<CancelReservationsCommandHandler> logger,
            ICrawlerPrefetchService crawlerPrefetchService,
            IDateTimeProvider dateTimeProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.crawlerPrefetchService = crawlerPrefetchService ?? throw new ArgumentNullException(nameof(crawlerPrefetchService));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task CancelReservations()
        {
            try
            {
                logger.LogInformation("Attempting to cancel all reservations for the crawler process...");

                await crawlerPrefetchService.CancelAllReservationsAsync(dateTimeProvider.UtcNow);

                logger.LogInformation("Successfully cancelled all reservations for the crawler process.");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed cancelled all reservations for the crawler process.");
            }
        }
    }
}