using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using StackExchange.Redis;
using SteamScrapper.Extensions;
using SteamScrapper.PageModels;
using SteamScrapper.Services;
using SteamScrapper.Utilities;

namespace SteamScrapper
{
    internal class Program
    {
        private static readonly IEnumerable<string> LinksAllowedForExploration = new HashSet<string>
        {
            "https://store.steampowered.com/",
            "https://store.steampowered.com/macos",
            "https://store.steampowered.com/linux",
        };

        private static readonly IEnumerable<string> LinkPrefixesAllowedForExploration = new[]
        {
            "https://store.steampowered.com/app/",
            "https://store.steampowered.com/bundle/",
            "https://store.steampowered.com/dlc/",
            "https://store.steampowered.com/sub/",
            //"https://store.steampowered.com/tags/",
            //"https://store.steampowered.com/explore/",
            //"https://store.steampowered.com/genre/",
            //"https://store.steampowered.com/recommended/",
            //"https://store.steampowered.com/sale/",
            //"https://store.steampowered.com/games/",
            //"https://store.steampowered.com/developer/",
            //"https://store.steampowered.com/publisher/",
            //"https://store.steampowered.com/franchise/",
            //"https://store.steampowered.com/controller/",
            //"https://store.steampowered.com/demos/",
            //"https://store.steampowered.com/specials/",
        };

        private static IDatabase redisDatabase;
        private static SteamService steamService = new SteamService();

        private static async Task Main(string[] args)
        {
            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync("host.docker.internal:6379");
            redisDatabase = connectionMultiplexer.GetDatabase(2);

            /*
            var gamePage = await GamePage.CreateAsync("https://store.steampowered.com/app/378648/The_Witcher_3_Wild_Hunt__Blood_and_Wine/");
            var subPage = await SubPage.CreateAsync("https://store.steampowered.com/sub/392522");
            var bundlePage = await BundlePage.CreateAsync("https://store.steampowered.com/bundle/12231/Shadow_of_the_Tomb_Raider_Definitive_Edition/");

            var s = gamePage.GetLinksForSubs();
            */

            var steamRootUri = new Uri("https://store.steampowered.com/", UriKind.Absolute);
            var crawler = new Crawler(redisDatabase, steamService, false);

            await crawler.DiscoverSteamLinksAsync(steamRootUri);

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit...");

            var _ = Console.ReadKey();
        }

        private static async Task DiscoverSteamLinksAsync(Uri startingUri)
        {
            var consoleOriginalForeground = Console.ForegroundColor;
            var redisKeyDateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            startingUri = LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(startingUri);

            await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:ToBeExplored", startingUri.AbsoluteUri);

            while (true)
            {
                redisKeyDateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

                var addressToProcessAbsUris = await redisDatabase.SetPopAsync($"Crawler:{redisKeyDateStamp}:ToBeExplored", 1);
                string addressToProcessAbsUri = addressToProcessAbsUris.ElementAtOrDefault(0);
                if (string.IsNullOrWhiteSpace(addressToProcessAbsUri))
                {
                    break;
                }

                var addressToProcess = new Uri(addressToProcessAbsUri, UriKind.Absolute);
                var isLinkNotYetExplored = await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored", addressToProcessAbsUri);
                if (!isLinkNotYetExplored)
                {
                    continue;
                }

                var steamPage = await steamService.CreateSteamPageAsync(addressToProcess);

                var helperSetId = $"Crawler:{redisKeyDateStamp}:HelperSets:{Guid.NewGuid():n}";
                var toBeExploredLinks = steamPage.NormalizedLinks.Where(uri => IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
                var ignoredLinks = steamPage.NormalizedLinks.Where(uri => !IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();

                var updateExplorationStatusTransaction = redisDatabase.CreateTransaction();

                var addToBeExploredToHelperSetTask = updateExplorationStatusTransaction.SetAddAsync(helperSetId, toBeExploredLinks);
                var addIgnoredLinksTask = updateExplorationStatusTransaction.SetAddAsync($"Crawler:{redisKeyDateStamp}:Ignored", ignoredLinks);

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

                Console.WriteLine(addressToProcessAbsUri);
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

        private static async Task DiscoverSteamLinks2Async(Uri address, HashSet<Uri> alreadyExplored)
        {
            var processingQueue = new Queue<Uri>();

            processingQueue.Enqueue(address);

            var redisKeyDateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            while (processingQueue.Count > 0)
            {
                var addressToProcess = processingQueue.Dequeue();
                var adresssToProcessAbsUri = addressToProcess.AbsoluteUri;

                if (!LinksAllowedForExploration.Contains(adresssToProcessAbsUri) && !LinkPrefixesAllowedForExploration.Any(x => adresssToProcessAbsUri.StartsWith(x)))
                {
                    await redisDatabase.SetAddAsync($"IgnoredLinks:{redisKeyDateStamp}", adresssToProcessAbsUri);
                    continue;
                }

                if (!alreadyExplored.Contains(addressToProcess))
                {
                    var pageHtml = await steamService.DownloadPageHtmlAsync(addressToProcess);
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(pageHtml);

                    var steamPage = new SteamPage(address, htmlDocument);

                    processingQueue.EnqueueRange(steamPage.NormalizedLinks);

                    var transaction = redisDatabase.CreateTransaction();

                    var addExploredLinkTask = transaction.SetAddAsync($"ExploredLinks:{redisKeyDateStamp}", adresssToProcessAbsUri);

                    Console.WriteLine(adresssToProcessAbsUri);
                    Console.WriteLine("  Found app links:");
                    foreach (var appLink in steamPage.AppLinks)
                    {
                        var appId = appLink.Segments.ElementAtOrDefault(2)?.TrimEnd('/');

                        if (!string.IsNullOrWhiteSpace(appId))
                        {
                            Console.WriteLine($"    App={appId}: {appLink}");
                            var addAppIdTask = transaction.SetAddAsync("Apps", appId.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                    Console.WriteLine("  Found bundle links:");
                    foreach (var bundleLink in steamPage.BundleLinks)
                    {
                        var bundleId = bundleLink.Segments.ElementAtOrDefault(2)?.TrimEnd('/');

                        if (!string.IsNullOrWhiteSpace(bundleId))
                        {
                            Console.WriteLine($"    Bundle={bundleId}: {bundleLink}");
                            var addBundleIdTask = transaction.SetAddAsync("Bundles", bundleId);
                        }
                    }

                    await transaction.ExecuteAsync();

                    alreadyExplored.Add(addressToProcess);
                }
            }
        }
    }
}