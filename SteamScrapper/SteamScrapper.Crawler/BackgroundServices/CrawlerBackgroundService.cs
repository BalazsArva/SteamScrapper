using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using SteamScrapper.Common.Constants;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Crawler.BackgroundServices
{
    public class CrawlerBackgroundService : BackgroundService
    {
        // TODO: Eventually remove nuget references to redis and other infra stuff.
        private static readonly IEnumerable<string> LinksAllowedForExploration = new HashSet<string>
        {
            PageUrls.SteamStore,

            // Note: these two usually don't have a trailing '/' in the HTML.
            "https://store.steampowered.com/linux",
            "https://store.steampowered.com/macos",
        };

        private static readonly IEnumerable<string> LinkPrefixesAllowedForExploration = new[]
        {
            PageUrlPrefixes.App,
            PageUrlPrefixes.Bundle,
            PageUrlPrefixes.Controller,
            PageUrlPrefixes.Demos,
            PageUrlPrefixes.Developer,
            PageUrlPrefixes.Dlc,
            PageUrlPrefixes.Explore,
            PageUrlPrefixes.Franchise,
            PageUrlPrefixes.Games,
            PageUrlPrefixes.Genre,
            PageUrlPrefixes.Publisher,
            PageUrlPrefixes.Recommended,
            PageUrlPrefixes.Sale,
            PageUrlPrefixes.Specials,
            PageUrlPrefixes.Sub,
            PageUrlPrefixes.Tags,
        };

        private readonly IDatabase redisDatabase;
        private readonly ISteamContentRegistrationService steamContentRegistrationService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly bool enableLoggingIgnoredLinks;

        public CrawlerBackgroundService(IDatabase redisDatabase, ISteamContentRegistrationService steamContentRegistrationService, ISteamPageFactory steamPageFactory, bool enableLoggingIgnoredLinks)
        {
            this.redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
            this.steamContentRegistrationService = steamContentRegistrationService ?? throw new ArgumentNullException(nameof(steamContentRegistrationService));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.enableLoggingIgnoredLinks = enableLoggingIgnoredLinks;
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
            var redisKeyDateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            var normalizedStartingUris = startingUris
                .Select(startingUri => LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(startingUri))
                .Select(startingUri => startingUri.AbsoluteUri)
                .Distinct()
                .Select(startingUri => new RedisValue(startingUri))
                .ToArray();

            await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:ToBeExplored", normalizedStartingUris);

            // TODO: Move the guts to a handler that executes a single iteration. This makes testing easier.
            while (!stoppingToken.IsCancellationRequested)
            {
                redisKeyDateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

                var addressToProcessUri = await GetNextUriToProcessAsync(redisKeyDateStamp);
                if (addressToProcessUri is null)
                {
                    break;
                }

                var canExploreLink = await TryRegisterLinkAsExploredAsync(redisKeyDateStamp, addressToProcessUri);
                if (!canExploreLink)
                {
                    // Link already explored, move on
                    continue;
                }

                var steamPage = await steamPageFactory.CreateSteamPageAsync(addressToProcessUri);

                var updateExplorationStatusTransaction = redisDatabase.CreateTransaction();
                var toBeExploredLinks = steamPage.NormalizedLinks.Where(uri => IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
                var helperSetId = $"Crawler:{redisKeyDateStamp}:HelperSets:{Guid.NewGuid():n}";

                var addToBeExploredToHelperSetTask = updateExplorationStatusTransaction.SetAddAsync(helperSetId, toBeExploredLinks);
                if (enableLoggingIgnoredLinks)
                {
                    var ignoredLinks = steamPage.NormalizedLinks.Where(uri => !IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
                    var addIgnoredLinksTask = updateExplorationStatusTransaction.SetAddAsync($"Crawler:{redisKeyDateStamp}:Ignored", ignoredLinks);
                }

                var notYetExploredRedisValsTask = updateExplorationStatusTransaction.SetCombineAsync(SetOperation.Difference, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored");
                var deleteHelperSetTask = updateExplorationStatusTransaction.KeyDeleteAsync(helperSetId);

                await updateExplorationStatusTransaction.ExecuteAsync();

                var notYetExploredLinks = notYetExploredRedisValsTask.Result.Select(val => (string)val).ToHashSet();
                if (notYetExploredLinks.Count == 0)
                {
                    continue;
                }

                await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:ToBeExplored", notYetExploredRedisValsTask.Result);

                Console.WriteLine(addressToProcessUri);

                await RegisterFoundAppsAsync(consoleOriginalForeground, steamPage, notYetExploredLinks);
                await RegisterFoundBundlesAsync(consoleOriginalForeground, steamPage, notYetExploredLinks);
                await RegisterFoundSubsAsync(consoleOriginalForeground, steamPage, notYetExploredLinks);
            }
        }

        private async Task RegisterFoundBundlesAsync(ConsoleColor consoleOriginalForeground, SteamPage steamPage, HashSet<string> notYetExploredLinks)
        {
            await steamContentRegistrationService.RegisterUnknownBundlesAsync(steamPage.BundleLinks.Select(x => x.BundleId));

            Console.WriteLine("  Found bundle links:");
            foreach (var bundleLink in steamPage.BundleLinks)
            {
                var bundleLinkIsNotYetExplored = notYetExploredLinks.Contains(bundleLink.Address.AbsoluteUri);

                Console.ForegroundColor = bundleLinkIsNotYetExplored ? ConsoleColor.Green : consoleOriginalForeground;
                Console.WriteLine($"    Bundle={bundleLink.BundleId}: {bundleLink.Address.AbsoluteUri}");
                Console.ForegroundColor = consoleOriginalForeground;
            }
        }

        private async Task RegisterFoundSubsAsync(ConsoleColor consoleOriginalForeground, SteamPage steamPage, HashSet<string> notYetExploredLinks)
        {
            await steamContentRegistrationService.RegisterUnknownSubsAsync(steamPage.SubLinks.Select(x => x.SubId));

            Console.WriteLine("  Found sub links:");
            foreach (var subLink in steamPage.SubLinks)
            {
                var subLinkIsNotYetExplored = notYetExploredLinks.Contains(subLink.Address.AbsoluteUri);

                Console.ForegroundColor = subLinkIsNotYetExplored ? ConsoleColor.Green : consoleOriginalForeground;
                Console.WriteLine($"    Sub={subLink.SubId}: {subLink.Address.AbsoluteUri}");
                Console.ForegroundColor = consoleOriginalForeground;
            }
        }

        private async Task RegisterFoundAppsAsync(ConsoleColor consoleOriginalForeground, SteamPage steamPage, HashSet<string> notYetExploredLinks)
        {
            await steamContentRegistrationService.RegisterUnknownAppsAsync(steamPage.AppLinks.Select(x => x.AppId));

            Console.WriteLine("  Found app links:");
            foreach (var appLink in steamPage.AppLinks)
            {
                var appLinkIsNotYetExplored = notYetExploredLinks.Contains(appLink.Address.AbsoluteUri);

                Console.ForegroundColor = appLinkIsNotYetExplored ? ConsoleColor.Green : consoleOriginalForeground;
                Console.WriteLine($"    App={appLink.AppId}: {appLink.Address.AbsoluteUri}");
                Console.ForegroundColor = consoleOriginalForeground;
            }
        }

        private static bool IsLinkAllowedForExploration(Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri;

            return
                LinksAllowedForExploration.Contains(absoluteUri) ||
                LinkPrefixesAllowedForExploration.Any(x => absoluteUri.StartsWith(x));
        }

        private async Task<bool> TryRegisterLinkAsExploredAsync(string redisKeyDateStamp, Uri addressToProcessUri)
        {
            // TODO: These are not yet usable, because the set difference used in the DiscoverSteamLinksAsync method relies on a single 'Explored' set.
            /*
            const int bitmapSize = 1024;

            var absoluteUri = addressToProcessUri.AbsoluteUri;

            if (absoluteUri.StartsWith(PageUrlPrefixes.App, StringComparison.OrdinalIgnoreCase))
            {
                var appId = Utilities.LinkHelpers.SteamLinkHelper.ExtractAppId(addressToProcessUri);

                var bitmapId = appId / bitmapSize;
                var bitmapOffset = appId % bitmapSize;

                return await redisDatabase.StringSetBitAsync($"Crawler:{redisKeyDateStamp}:Explored:AppBitmaps:{bitmapId}", bitmapOffset, true);
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Sub, StringComparison.OrdinalIgnoreCase))
            {
                var subId = Utilities.LinkHelpers.SteamLinkHelper.ExtractSubId(addressToProcessUri);

                var bitmapId = subId / bitmapSize;
                var bitmapOffset = subId % bitmapSize;

                return await redisDatabase.StringSetBitAsync($"Crawler:{redisKeyDateStamp}:Explored:SubBitmaps:{bitmapId}", bitmapOffset, true);
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Bundle, StringComparison.OrdinalIgnoreCase))
            {
                var bundleId = Utilities.LinkHelpers.SteamLinkHelper.ExtractBundleId(addressToProcessUri);

                var bitmapId = bundleId / bitmapSize;
                var bitmapOffset = bundleId % bitmapSize;

                return await redisDatabase.StringSetBitAsync($"Crawler:{redisKeyDateStamp}:Explored:BundleBitmaps:{bitmapId}", bitmapOffset, true);
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Developer, StringComparison.OrdinalIgnoreCase))
            {
                return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:Developer", addressToProcessUri.AbsoluteUri);
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Publisher, StringComparison.OrdinalIgnoreCase))
            {
                return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:Publisher", addressToProcessUri.AbsoluteUri);
            }
            */

            return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:EverythingElse", addressToProcessUri.AbsoluteUri);
        }

        private async Task<Uri> GetNextUriToProcessAsync(string redisKeyDateStamp)
        {
            var addressToProcessAbsUris = await redisDatabase.SetPopAsync($"Crawler:{redisKeyDateStamp}:ToBeExplored", 1);
            string addressToProcessAbsUri = addressToProcessAbsUris.ElementAtOrDefault(0);
            if (string.IsNullOrWhiteSpace(addressToProcessAbsUri))
            {
                return null;
            }

            return new Uri(addressToProcessAbsUri.Trim(), UriKind.Absolute);
        }
    }
}