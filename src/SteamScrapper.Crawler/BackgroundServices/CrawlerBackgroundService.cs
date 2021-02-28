﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Crawler.BackgroundServices
{
    public class CrawlerBackgroundService : BackgroundService
    {
        private readonly ILogger<CrawlerBackgroundService> logger;
        private readonly ICrawlerAddressRegistrationService crawlerAddressRegistrationService;
        private readonly ICrawlerPrefetchService crawlerPrefetchService;
        private readonly ISteamContentRegistrationService steamContentRegistrationService;

        public CrawlerBackgroundService(
            ILogger<CrawlerBackgroundService> logger,
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService,
            ICrawlerPrefetchService crawlerPrefetchService,
            ISteamContentRegistrationService steamContentRegistrationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.crawlerAddressRegistrationService = crawlerAddressRegistrationService ?? throw new ArgumentNullException(nameof(crawlerAddressRegistrationService));
            this.crawlerPrefetchService = crawlerPrefetchService ?? throw new ArgumentNullException(nameof(crawlerPrefetchService));
            this.steamContentRegistrationService = steamContentRegistrationService ?? throw new ArgumentNullException(nameof(steamContentRegistrationService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var utcNow = DateTime.UtcNow;

            await RegisterStartingAddressesAsync(utcNow);

            // TODO: Move the guts to a handler that executes a single iteration. This makes testing easier.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();

                    utcNow = DateTime.UtcNow;

                    var steamPage = await crawlerPrefetchService.GetNextPageAsync(utcNow);
                    if (steamPage is null)
                    {
                        // No links remain to explore.
                        // TODO: Restart the next day. But check cancellation token as well.
                        break;
                    }

                    var notYetExploredLinks = await crawlerAddressRegistrationService.RegisterNonExploredLinksForExplorationAsync(utcNow, steamPage.NormalizedLinks);

                    var unknownApps = await RegisterFoundAppsAsync(steamPage, notYetExploredLinks);
                    var unknownBundles = await RegisterFoundBundlesAsync(steamPage, notYetExploredLinks);
                    var unknownSubs = await RegisterFoundSubsAsync(steamPage, notYetExploredLinks);

                    stopwatch.Stop();

                    logger.LogInformation(
                        "Processed URL '{@Url}'. Elapsed millis: {@ElapsedMillis}, Found {@NotExploredAppCount} not explored apps, {@NotKnownAppCount} not known apps, " +
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
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An unhandled error occurred during the crawling process.");
                }
            }

            logger.LogInformation("Finished crawling.");
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

        private async Task RegisterStartingAddressesAsync(DateTime utcNow)
        {
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