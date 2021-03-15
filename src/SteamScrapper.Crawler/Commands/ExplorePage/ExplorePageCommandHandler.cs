using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Exceptions;

namespace SteamScrapper.Crawler.Commands.ExplorePage
{
    public class ExplorePageCommandHandler : IExplorePageCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILogger logger;
        private readonly ICrawlerAddressRegistrationService crawlerAddressRegistrationService;
        private readonly ICrawlerPrefetchService crawlerPrefetchService;
        private readonly ISteamContentRegistrationService steamContentRegistrationService;

        public ExplorePageCommandHandler(
            IDateTimeProvider dateTimeProvider,
            ILogger<ExplorePageCommandHandler> logger,
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService,
            ICrawlerPrefetchService crawlerPrefetchService,
            ISteamContentRegistrationService steamContentRegistrationService)
        {
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.crawlerAddressRegistrationService = crawlerAddressRegistrationService ?? throw new ArgumentNullException(nameof(crawlerAddressRegistrationService));
            this.crawlerPrefetchService = crawlerPrefetchService ?? throw new ArgumentNullException(nameof(crawlerPrefetchService));
            this.steamContentRegistrationService = steamContentRegistrationService ?? throw new ArgumentNullException(nameof(steamContentRegistrationService));
        }

        public async Task<ExplorePageCommandResult> ExplorePageAsync(CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var utcNow = dateTimeProvider.UtcNow;

                var steamPage = await crawlerPrefetchService.GetNextPageAsync(utcNow);
                if (steamPage is null)
                {
                    // No links remain to explore.
                    // TODO: Restart the next day. But check cancellation token as well.
                    return ExplorePageCommandResult.NoMoreItems;
                }

                var notYetExploredLinks = await crawlerAddressRegistrationService.RegisterNonExploredLinksForExplorationAsync(utcNow, steamPage.NormalizedLinks);

                var unknownApps = await RegisterFoundAppsAsync(steamPage, notYetExploredLinks);
                var unknownBundles = await RegisterFoundBundlesAsync(steamPage, notYetExploredLinks);
                var unknownSubs = await RegisterFoundSubsAsync(steamPage, notYetExploredLinks);

                stopwatch.Stop();

                logger.LogInformation(
                    "Processed URI '{@Uri}'. Elapsed millis: {@ElapsedMillis}, Found {@NotExploredAppCount} not explored apps, {@NotKnownAppCount} not known apps, " +
                    "{@NotExploredSubCount} not explored subs, {@NotKnownSubCount} not known subs and " +
                    "{@NotExploredBundleCount} not explored bundles, {@NotKnownBundleCount} not known bundles.",
                    steamPage.NormalizedAddress.AbsoluteUri,
                    stopwatch.ElapsedMilliseconds,
                    unknownApps.NotYetExploredCount,
                    unknownApps.NotYetKnownCount,
                    unknownSubs.NotYetExploredCount,
                    unknownSubs.NotYetKnownCount,
                    unknownBundles.NotYetExploredCount,
                    unknownBundles.NotYetKnownCount);

                return ExplorePageCommandResult.Success;
            }
            catch (SteamPageRemovedException e)
            {
                logger.LogWarning("The page located at URL {@Uri} has been removed.", e.Uri);

                return ExplorePageCommandResult.Success;
            }
        }

        private async Task<(int NotYetExploredCount, int NotYetKnownCount)> RegisterFoundBundlesAsync(SteamPage steamPage, ISet<string> notYetExploredLinks)
        {
            var unknownBundleIds = new List<int>(notYetExploredLinks.Count);

            foreach (var bundleLink in steamPage.BundleLinks)
            {
                var bundleLinkIsNotYetExplored = notYetExploredLinks.Contains(bundleLink.Address.AbsoluteUri);
                if (bundleLinkIsNotYetExplored)
                {
                    unknownBundleIds.Add(bundleLink.BundleId);
                }
            }

            var notYetKnownCount = await steamContentRegistrationService.RegisterUnknownBundlesAsync(unknownBundleIds);

            return (unknownBundleIds.Count, notYetKnownCount);
        }

        private async Task<(int NotYetExploredCount, int NotYetKnownCount)> RegisterFoundSubsAsync(SteamPage steamPage, ISet<string> notYetExploredLinks)
        {
            var unknownSubIds = new List<int>(notYetExploredLinks.Count);

            foreach (var subLink in steamPage.SubLinks)
            {
                var subLinkIsNotYetExplored = notYetExploredLinks.Contains(subLink.Address.AbsoluteUri);
                if (subLinkIsNotYetExplored)
                {
                    unknownSubIds.Add(subLink.SubId);
                }
            }

            var notYetKnownCount = await steamContentRegistrationService.RegisterUnknownSubsAsync(unknownSubIds);

            return (unknownSubIds.Count, notYetKnownCount);
        }

        private async Task<(int NotYetExploredCount, int NotYetKnownCount)> RegisterFoundAppsAsync(SteamPage steamPage, ISet<string> notYetExploredLinks)
        {
            var unknownAppIds = new List<int>(notYetExploredLinks.Count);

            foreach (var appLink in steamPage.AppLinks)
            {
                var appLinkIsNotYetExplored = notYetExploredLinks.Contains(appLink.Address.AbsoluteUri);
                if (appLinkIsNotYetExplored)
                {
                    unknownAppIds.Add(appLink.AppId);
                }
            }

            var notYetKnownCount = await steamContentRegistrationService.RegisterUnknownAppsAsync(unknownAppIds);

            return (unknownAppIds.Count, notYetKnownCount);
        }
    }
}