using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Common.Constants;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Infrastructure.Services
{
    public class CrawlerAddressRegistrationService : ICrawlerAddressRegistrationService
    {
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

        // TODO: Config
        private readonly bool EnableLoggingIgnoredLinks = false;

        private readonly IDatabase redisDatabase;

        public CrawlerAddressRegistrationService(IConnectionMultiplexer connectionMultiplexer)
        {
            if (connectionMultiplexer is null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }

            redisDatabase = connectionMultiplexer.GetDatabase();
        }

        public async Task<Uri> GetNextAddressAsync(DateTime executionDate, CancellationToken cancellationToken)
        {
            var redisKeyDateStamp = executionDate.ToString("yyyyMMdd");

            while (!cancellationToken.IsCancellationRequested)
            {
                var addressToProcessAbsUris = await redisDatabase.SetPopAsync($"Crawler:{redisKeyDateStamp}:ToBeExplored", 1);

                string addressToProcessAbsUri = addressToProcessAbsUris.ElementAtOrDefault(0);

                // Ran out of items to process
                if (string.IsNullOrWhiteSpace(addressToProcessAbsUri))
                {
                    return null;
                }

                var result = new Uri(addressToProcessAbsUri.Trim(), UriKind.Absolute);
                if (await TryRegisterLinkAsExploredAsync(redisKeyDateStamp, result))
                {
                    return result;
                }
            }

            return null;
        }

        public async Task<ISet<string>> RegisterNonExploredLinksForExplorationAsync(DateTime executionDate, IEnumerable<Uri> foundLinks)
        {
            if (foundLinks is null)
            {
                throw new ArgumentNullException(nameof(foundLinks));
            }

            if (!foundLinks.Any())
            {
                return new HashSet<string>();
            }

            var redisKeyDateStamp = executionDate.ToString("yyyyMMdd");

            var updateExplorationStatusTransaction = redisDatabase.CreateTransaction();
            var toBeExploredLinks = foundLinks.Where(uri => IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
            var helperSetId = $"Crawler:{redisKeyDateStamp}:HelperSets:{Guid.NewGuid():n}";

            var addToBeExploredToHelperSetTask = updateExplorationStatusTransaction.SetAddAsync(helperSetId, toBeExploredLinks);
            if (EnableLoggingIgnoredLinks)
            {
                var ignoredLinks = foundLinks.Where(uri => !IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
                var addIgnoredLinksTask = updateExplorationStatusTransaction.SetAddAsync($"Crawler:{redisKeyDateStamp}:Ignored", ignoredLinks);
            }

            var notYetExploredRedisValsTask = updateExplorationStatusTransaction.SetCombineAsync(SetOperation.Difference, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored");
            var deleteHelperSetTask = updateExplorationStatusTransaction.KeyDeleteAsync(helperSetId);

            await updateExplorationStatusTransaction.ExecuteAsync();

            var notYetExploredLinks = notYetExploredRedisValsTask.Result.Select(val => (string)val).ToHashSet();
            if (notYetExploredLinks.Count != 0)
            {
                await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:ToBeExplored", notYetExploredRedisValsTask.Result);
            }

            return notYetExploredLinks;
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

            return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:EverythingElse", addressToProcessUri.AbsoluteUri);
            */

            return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored", addressToProcessUri.AbsoluteUri);
        }

        private static bool IsLinkAllowedForExploration(Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri;

            return
                LinksAllowedForExploration.Contains(absoluteUri) ||
                LinkPrefixesAllowedForExploration.Any(x => absoluteUri.StartsWith(x));
        }
    }
}