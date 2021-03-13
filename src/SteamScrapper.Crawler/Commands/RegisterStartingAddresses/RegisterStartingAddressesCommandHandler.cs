using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SteamScrapper.Common.Providers;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Crawler.Options;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Crawler.Commands.RegisterStartingAddresses
{
    public class RegisterStartingAddressesCommandHandler : IRegisterStartingAddressesCommandHandler
    {
        private readonly RegisterStartingAddressesOptions options;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ICrawlerAddressRegistrationService crawlerAddressRegistrationService;

        public RegisterStartingAddressesCommandHandler(
            IOptions<RegisterStartingAddressesOptions> options,
            IDateTimeProvider dateTimeProvider,
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.crawlerAddressRegistrationService = crawlerAddressRegistrationService ?? throw new ArgumentNullException(nameof(crawlerAddressRegistrationService));

            var startingAddresses = options.Value.StartingAddresses;
            if (startingAddresses is null || startingAddresses.Length == 0)
            {
                throw new ArgumentException("At least 1 starting address must be configured.", nameof(startingAddresses));
            }
        }

        public async Task RegisterStartingAddressesAsync(CancellationToken cancellationToken)
        {
            var utcNow = dateTimeProvider.UtcNow;
            var normalizedStartingUris = options.StartingAddresses
                .Select(startingUri => LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(startingUri))
                .ToList();

            await crawlerAddressRegistrationService.RegisterNonExploredLinksForExplorationAsync(utcNow, normalizedStartingUris);
        }
    }
}