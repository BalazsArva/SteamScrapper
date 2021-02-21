using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Factories;
using SteamScrapper.Utilities;

namespace SteamScrapper
{
    public class Crawler
    {
        private static readonly IEnumerable<string> LinksAllowedForExploration = new HashSet<string>
        {
            "https://store.steampowered.com/",
            "https://store.steampowered.com/linux",
            "https://store.steampowered.com/macos",
        };

        private static readonly IEnumerable<string> LinkPrefixesAllowedForExploration = new[]
        {
            "https://store.steampowered.com/app/",
            "https://store.steampowered.com/bundle/",
            "https://store.steampowered.com/controller/",
            "https://store.steampowered.com/demos/",
            "https://store.steampowered.com/developer/",
            "https://store.steampowered.com/dlc/",
            "https://store.steampowered.com/explore/",
            "https://store.steampowered.com/franchise/",
            "https://store.steampowered.com/games/",
            "https://store.steampowered.com/genre/",
            "https://store.steampowered.com/publisher/",
            "https://store.steampowered.com/recommended/",
            "https://store.steampowered.com/sale/",
            "https://store.steampowered.com/specials/",
            "https://store.steampowered.com/sub/",
            "https://store.steampowered.com/tags/",
        };

        private readonly IDatabase redisDatabase;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly bool enableLoggingIgnoredLinks;

        public Crawler(IDatabase redisDatabase, ISteamPageFactory steamPageFactory, bool enableLoggingIgnoredLinks)
        {
            this.redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.enableLoggingIgnoredLinks = enableLoggingIgnoredLinks;
        }

        public async Task DiscoverSteamLinksAsync(IEnumerable<Uri> startingUris)
        {
            if (startingUris is null)
            {
                throw new ArgumentNullException(nameof(startingUris));
            }

            var consoleOriginalForeground = Console.ForegroundColor;
            var redisKeyDateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            var normalizedStartingUris = startingUris
                .Select(startingUri => LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(startingUri))
                .Select(startingUri => startingUri.AbsoluteUri)
                .Distinct()
                .Select(startingUri => new RedisValue(startingUri))
                .ToArray();

            await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:ToBeExplored", normalizedStartingUris);

            while (true)
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

                var helperSetId = $"Crawler:{redisKeyDateStamp}:HelperSets:{Guid.NewGuid():n}";
                var toBeExploredLinks = steamPage.NormalizedLinks.Where(uri => IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
                var ignoredLinks = steamPage.NormalizedLinks.Where(uri => !IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();

                var updateExplorationStatusTransaction = redisDatabase.CreateTransaction();

                var addToBeExploredToHelperSetTask = updateExplorationStatusTransaction.SetAddAsync(helperSetId, toBeExploredLinks);
                if (enableLoggingIgnoredLinks)
                {
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

                var registerFoundItemsTransaction = redisDatabase.CreateTransaction();

                Console.WriteLine(addressToProcessUri);
                Console.WriteLine("  Found app links:");
                foreach (var appLink in steamPage.AppLinks)
                {
                    var appId = appLink.Segments.ElementAtOrDefault(2)?.TrimEnd('/');
                    var appLinkIsNotYetExplored = notYetExploredLinks.Contains(appLink.AbsoluteUri);

                    if (!string.IsNullOrWhiteSpace(appId))
                    {
                        Console.ForegroundColor = appLinkIsNotYetExplored ? ConsoleColor.Green : consoleOriginalForeground;
                        Console.WriteLine($"    App={appId}: {appLink}");
                        Console.ForegroundColor = consoleOriginalForeground;

                        if (appLinkIsNotYetExplored)
                        {
                            var addAppIdTask = registerFoundItemsTransaction.SetAddAsync("Apps", appId.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }
                Console.WriteLine("  Found bundle links:");
                foreach (var bundleLink in steamPage.BundleLinks)
                {
                    var bundleId = bundleLink.Segments.ElementAtOrDefault(2)?.TrimEnd('/');
                    var bundleLinkIsNotYetExplored = notYetExploredLinks.Contains(bundleLink.AbsoluteUri);

                    if (!string.IsNullOrWhiteSpace(bundleId))
                    {
                        Console.ForegroundColor = bundleLinkIsNotYetExplored ? ConsoleColor.Green : consoleOriginalForeground;
                        Console.WriteLine($"    Bundle={bundleId}: {bundleLink}");
                        Console.ForegroundColor = consoleOriginalForeground;

                        if (bundleLinkIsNotYetExplored)
                        {
                            var addBundleIdTask = registerFoundItemsTransaction.SetAddAsync("Bundles", bundleId);
                        }
                    }
                }

                await registerFoundItemsTransaction.ExecuteAsync();
            }
        }

        private static bool IsLinkAllowedForExploration(Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri;

            return LinksAllowedForExploration.Contains(absoluteUri) || LinkPrefixesAllowedForExploration.Any(x => absoluteUri.StartsWith(x));
        }

        private async Task<bool> TryRegisterLinkAsExploredAsync(string redisKeyDateStamp, Uri addressToProcessUri)
        {
            return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored", addressToProcessUri.AbsoluteUri);
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