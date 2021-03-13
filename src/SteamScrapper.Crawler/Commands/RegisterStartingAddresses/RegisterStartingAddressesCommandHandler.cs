using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamScrapper.Common.Providers;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Crawler.Commands.RegisterStartingAddresses
{
    public class RegisterStartingAddressesCommandHandler : IRegisterStartingAddressesCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ICrawlerAddressRegistrationService crawlerAddressRegistrationService;

        public RegisterStartingAddressesCommandHandler(
            IDateTimeProvider dateTimeProvider,
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService)
        {
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.crawlerAddressRegistrationService = crawlerAddressRegistrationService ?? throw new ArgumentNullException(nameof(crawlerAddressRegistrationService));
        }

        public async Task RegisterStartingAddressesAsync(CancellationToken cancellationToken)
        {
            var utcNow = dateTimeProvider.UtcNow;

            // TODO: Move to config
            IEnumerable<Uri> startingUris = new[]
            {
                new Uri("https://store.steampowered.com/", UriKind.Absolute),
                new Uri("https://store.steampowered.com/developer/", UriKind.Absolute),
                new Uri("https://store.steampowered.com/publisher/", UriKind.Absolute),
            };

            var normalizedStartingUris = startingUris
                .Select(startingUri => LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(startingUri))
                .ToList();

            await crawlerAddressRegistrationService.RegisterNonExploredLinksForExplorationAsync(utcNow, normalizedStartingUris);
        }
    }
}