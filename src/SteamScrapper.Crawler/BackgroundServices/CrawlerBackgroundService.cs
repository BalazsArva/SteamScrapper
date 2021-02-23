using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Crawler.BackgroundServices
{
    // TODO: Eventually remove nuget references to redis and other infra stuff.
    public class CrawlerBackgroundService : BackgroundService
    {
        private readonly ICrawlerAddressRegistrationService crawlerAddressRegistrationService;
        private readonly ISteamContentRegistrationService steamContentRegistrationService;
        private readonly ISteamPageFactory steamPageFactory;

        public CrawlerBackgroundService(
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService,
            ISteamContentRegistrationService steamContentRegistrationService,
            ISteamPageFactory steamPageFactory)
        {
            this.crawlerAddressRegistrationService = crawlerAddressRegistrationService ?? throw new ArgumentNullException(nameof(crawlerAddressRegistrationService));
            this.steamContentRegistrationService = steamContentRegistrationService ?? throw new ArgumentNullException(nameof(steamContentRegistrationService));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TODO: Move to config
            IEnumerable<Uri> startingUris = new[]
            {
                new Uri("https://store.steampowered.com/", UriKind.Absolute),
                new Uri("https://store.steampowered.com/developer/", UriKind.Absolute),
                new Uri("https://store.steampowered.com/publisher/", UriKind.Absolute),
            };

            var consoleOriginalForeground = Console.ForegroundColor;
            var utcNow = DateTime.UtcNow;
            var redisKeyDateStamp = utcNow.ToString("yyyyMMdd");

            var normalizedStartingUris = startingUris
                .Select(startingUri => LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(startingUri))
                .ToList();

            await crawlerAddressRegistrationService.RegisterNonExploredLinksForExplorationAsync(utcNow, normalizedStartingUris);

            // TODO: Move the guts to a handler that executes a single iteration. This makes testing easier.
            while (!stoppingToken.IsCancellationRequested)
            {
                utcNow = DateTime.UtcNow;
                redisKeyDateStamp = utcNow.ToString("yyyyMMdd");

                var addressToProcessUri = await crawlerAddressRegistrationService.GetNextAddressAsync(utcNow);
                if (addressToProcessUri is null)
                {
                    // No links remain to explore.
                    // TODO: Restart the next day.
                    break;
                }

                var steamPage = await steamPageFactory.CreateSteamPageAsync(addressToProcessUri);

                var notYetExploredLinks = await crawlerAddressRegistrationService.RegisterNonExploredLinksForExplorationAsync(utcNow, steamPage.NormalizedLinks);

                Console.WriteLine(addressToProcessUri);

                await RegisterFoundAppsAsync(consoleOriginalForeground, steamPage, notYetExploredLinks);
                await RegisterFoundBundlesAsync(consoleOriginalForeground, steamPage, notYetExploredLinks);
                await RegisterFoundSubsAsync(consoleOriginalForeground, steamPage, notYetExploredLinks);
            }
        }

        private async Task RegisterFoundBundlesAsync(ConsoleColor consoleOriginalForeground, SteamPage steamPage, ISet<string> notYetExploredLinks)
        {
            var unknownBundleIds = new List<int>(notYetExploredLinks.Count);

            Console.WriteLine("  Found bundle links:");
            foreach (var bundleLink in steamPage.BundleLinks)
            {
                var bundleLinkIsNotYetExplored = notYetExploredLinks.Contains(bundleLink.Address.AbsoluteUri);

                if (bundleLinkIsNotYetExplored)
                {
                    unknownBundleIds.Add(bundleLink.BundleId);
                }

                Console.ForegroundColor = bundleLinkIsNotYetExplored ? ConsoleColor.Green : consoleOriginalForeground;
                Console.WriteLine($"    Bundle={bundleLink.BundleId}: {bundleLink.Address.AbsoluteUri}");
                Console.ForegroundColor = consoleOriginalForeground;
            }

            await steamContentRegistrationService.RegisterUnknownBundlesAsync(unknownBundleIds);
        }

        private async Task RegisterFoundSubsAsync(ConsoleColor consoleOriginalForeground, SteamPage steamPage, ISet<string> notYetExploredLinks)
        {
            var unknownSubIds = new List<int>(notYetExploredLinks.Count);

            Console.WriteLine("  Found sub links:");
            foreach (var subLink in steamPage.SubLinks)
            {
                var subLinkIsNotYetExplored = notYetExploredLinks.Contains(subLink.Address.AbsoluteUri);

                if (subLinkIsNotYetExplored)
                {
                    unknownSubIds.Add(subLink.SubId);
                }

                Console.ForegroundColor = subLinkIsNotYetExplored ? ConsoleColor.Green : consoleOriginalForeground;
                Console.WriteLine($"    Sub={subLink.SubId}: {subLink.Address.AbsoluteUri}");
                Console.ForegroundColor = consoleOriginalForeground;
            }

            await steamContentRegistrationService.RegisterUnknownSubsAsync(unknownSubIds);
        }

        private async Task RegisterFoundAppsAsync(ConsoleColor consoleOriginalForeground, SteamPage steamPage, ISet<string> notYetExploredLinks)
        {
            var unknownAppIds = new List<int>(notYetExploredLinks.Count);

            Console.WriteLine("  Found app links:");
            foreach (var appLink in steamPage.AppLinks)
            {
                var appLinkIsNotYetExplored = notYetExploredLinks.Contains(appLink.Address.AbsoluteUri);

                if (appLinkIsNotYetExplored)
                {
                    unknownAppIds.Add(appLink.AppId);
                }

                Console.ForegroundColor = appLinkIsNotYetExplored ? ConsoleColor.Green : consoleOriginalForeground;
                Console.WriteLine($"    App={appLink.AppId}: {appLink.Address.AbsoluteUri}");
                Console.ForegroundColor = consoleOriginalForeground;
            }

            await steamContentRegistrationService.RegisterUnknownAppsAsync(unknownAppIds);
        }
    }
}