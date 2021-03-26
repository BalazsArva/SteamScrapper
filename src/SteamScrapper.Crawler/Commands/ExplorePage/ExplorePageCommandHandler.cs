using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Repositories;
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
        private readonly IAppWriteRepository appRepository;
        private readonly IBundleWriteRepository bundleRepository;
        private readonly ISubWriteRepository subWriteRepository;

        public ExplorePageCommandHandler(
            IDateTimeProvider dateTimeProvider,
            ILogger<ExplorePageCommandHandler> logger,
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService,
            ICrawlerPrefetchService crawlerPrefetchService,
            IAppWriteRepository appRepository,
            IBundleWriteRepository bundleRepository,
            ISubWriteRepository subWriteRepository)
        {
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.crawlerAddressRegistrationService = crawlerAddressRegistrationService ?? throw new ArgumentNullException(nameof(crawlerAddressRegistrationService));
            this.crawlerPrefetchService = crawlerPrefetchService ?? throw new ArgumentNullException(nameof(crawlerPrefetchService));
            this.appRepository = appRepository ?? throw new ArgumentNullException(nameof(appRepository));
            this.bundleRepository = bundleRepository ?? throw new ArgumentNullException(nameof(bundleRepository));
            this.subWriteRepository = subWriteRepository ?? throw new ArgumentNullException(nameof(subWriteRepository));
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
            var unknownBundleIds = new List<long>(notYetExploredLinks.Count);

            foreach (var bundleLink in steamPage.BundleLinks)
            {
                var bundleLinkIsNotYetExplored = notYetExploredLinks.Contains(bundleLink.Address.AbsoluteUri);
                if (bundleLinkIsNotYetExplored)
                {
                    unknownBundleIds.Add(bundleLink.BundleId);
                }
            }

            var notYetKnownCount = await bundleRepository.RegisterUnknownBundlesAsync(unknownBundleIds);

            return (unknownBundleIds.Count, notYetKnownCount);
        }

        private async Task<(int NotYetExploredCount, int NotYetKnownCount)> RegisterFoundSubsAsync(SteamPage steamPage, ISet<string> notYetExploredLinks)
        {
            var unknownSubIds = new List<long>(notYetExploredLinks.Count);

            foreach (var subLink in steamPage.SubLinks)
            {
                var subLinkIsNotYetExplored = notYetExploredLinks.Contains(subLink.Address.AbsoluteUri);
                if (subLinkIsNotYetExplored)
                {
                    unknownSubIds.Add(subLink.SubId);
                }
            }

            var notYetKnownCount = await subWriteRepository.RegisterUnknownSubsAsync(unknownSubIds);

            return (unknownSubIds.Count, notYetKnownCount);
        }

        private async Task<(int NotYetExploredCount, int NotYetKnownCount)> RegisterFoundAppsAsync(SteamPage steamPage, ISet<string> notYetExploredLinks)
        {
            var unknownAppIds = new List<long>(notYetExploredLinks.Count);

            foreach (var appLink in steamPage.AppLinks)
            {
                var appLinkIsNotYetExplored = notYetExploredLinks.Contains(appLink.Address.AbsoluteUri);
                if (appLinkIsNotYetExplored)
                {
                    unknownAppIds.Add(appLink.AppId);
                }
            }

            var notYetKnownCount = await appRepository.RegisterUnknownAppsAsync(unknownAppIds);

            return (unknownAppIds.Count, notYetKnownCount);
        }
    }
}