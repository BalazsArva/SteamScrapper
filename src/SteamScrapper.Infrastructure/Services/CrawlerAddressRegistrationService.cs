using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using SteamScrapper.Common.DataStructures;
using SteamScrapper.Common.Urls;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.Redis;

namespace SteamScrapper.Infrastructure.Services
{
    public class CrawlerAddressRegistrationService : ICrawlerAddressRegistrationService
    {
        // It's sure there are Ids > 2 million, so 3 * 1000 * 1000 should be okay.
        private const int DefaultBitmapSize = 3 * 1000 * 1000;

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

        private readonly Bitmap exploredAppIds;
        private readonly Bitmap exploredSubIds;
        private readonly Bitmap exploredBundleIds;
        private readonly IDatabase redisDatabase;
        private readonly bool EnableRecordingIgnoredLinks;

        public CrawlerAddressRegistrationService(
            ILogger<CrawlerAddressRegistrationService> logger,
            IRedisConnectionWrapper redisConnectionWrapper,
            IOptions<CrawlerAddressRegistrationOptions> options)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Value is null)
            {
                throw new ArgumentException(
                    "The provided configuration object does not contain valid settings for crawler address registration.",
                    nameof(options));
            }

            exploredBundleIds = new Bitmap(DefaultBitmapSize);
            EnableRecordingIgnoredLinks = options.Value.EnableRecordingIgnoredLinks;
            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            // TODO: Sub and bundle ids may not need as large bitmap as apps do.
            exploredAppIds = new Bitmap(DefaultBitmapSize);
            exploredSubIds = new Bitmap(DefaultBitmapSize);

            var enableRecordingIgnoredLinksInfoText = EnableRecordingIgnoredLinks ? "enabled" : "disabled";
            logger.LogInformation($"Recording ignored links is {enableRecordingIgnoredLinksInfoText}.");
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
            var toBeExploredLinks = foundLinks.Where(uri => IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
            var helperSetId = $"Crawler:{redisKeyDateStamp}:HelperSets:{Guid.NewGuid():n}";

            var updateExplorationStatusTransaction = redisDatabase.CreateTransaction();

            var addToBeExploredToHelperSetTask = updateExplorationStatusTransaction.SetAddAsync(helperSetId, toBeExploredLinks);
            if (EnableRecordingIgnoredLinks)
            {
                var ignoredLinks = foundLinks.Where(uri => !IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
                var addIgnoredLinksTask = updateExplorationStatusTransaction.SetAddAsync($"Crawler:{redisKeyDateStamp}:Ignored", ignoredLinks);
            }

            // var notYetExploredRedisValsTask = updateExplorationStatusTransaction.SetCombineAsync(SetOperation.Difference, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored");
            var t1 = updateExplorationStatusTransaction.SetCombineAndStoreAsync(SetOperation.Difference, helperSetId, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored");
            var t2 = updateExplorationStatusTransaction.SetCombineAndStoreAsync(SetOperation.Difference, helperSetId, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored:Apps");
            var t3 = updateExplorationStatusTransaction.SetCombineAndStoreAsync(SetOperation.Difference, helperSetId, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored:Subs");
            var t4 = updateExplorationStatusTransaction.SetCombineAndStoreAsync(SetOperation.Difference, helperSetId, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored:Bundles");
            var t5 = updateExplorationStatusTransaction.SetCombineAndStoreAsync(SetOperation.Difference, helperSetId, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored:Developer");
            var t6 = updateExplorationStatusTransaction.SetCombineAndStoreAsync(SetOperation.Difference, helperSetId, helperSetId, $"Crawler:{redisKeyDateStamp}:Explored:Publisher");

            var notYetExploredRedisValsTask = updateExplorationStatusTransaction.SetMembersAsync(helperSetId);

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
            var absoluteUri = addressToProcessUri.AbsoluteUri;

            if (absoluteUri.StartsWith(PageUrlPrefixes.App, StringComparison.OrdinalIgnoreCase))
            {
                var appId = SteamLinkHelper.ExtractAppId(addressToProcessUri);

                if (exploredAppIds.Get(appId))
                {
                    // We know from local data that it's already explored. Don't even check Redis.
                    return false;
                }

                // If local data says that it's not yet explored, then remote data may disagree, other instances may have explored it.
                // So go out to Redis to check it.
                var couldRegisterForExploration = await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:Apps", addressToProcessUri.AbsoluteUri);

                // Always true. If we could register it against remote data, then we have just reserved it successfully,
                // and if we could not reserve it, then another instance has already reserved it, so by setting it to true,
                // we sync the local data.
                exploredAppIds.Set(appId, true);

                return couldRegisterForExploration;
            }

            // TODO: Instead of checking starts with all the time, could always receive an URL holder (and non-app/sub/bundle/etc. types could have a generic link class).
            // This could improve performance a tiny bit.
            if (absoluteUri.StartsWith(PageUrlPrefixes.Sub, StringComparison.OrdinalIgnoreCase))
            {
                var subId = SteamLinkHelper.ExtractSubId(addressToProcessUri);

                if (exploredSubIds.Get(subId))
                {
                    // We know from local data that it's already explored. Don't even check Redis.
                    return false;
                }

                // If local data says that it's not yet explored, then remote data may disagree, other instances may have explored it.
                // So go out to Redis to check it.
                var couldRegisterForExploration = await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:Subs", addressToProcessUri.AbsoluteUri);

                // Always true. If we could register it against remote data, then we have just reserved it successfully,
                // and if we could not reserve it, then another instance has already reserved it, so by setting it to true,
                // we sync the local data.
                exploredSubIds.Set(subId, true);

                return couldRegisterForExploration;
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Bundle, StringComparison.OrdinalIgnoreCase))
            {
                var bundleId = SteamLinkHelper.ExtractBundleId(addressToProcessUri);

                if (exploredBundleIds.Get(bundleId))
                {
                    // We know from local data that it's already explored. Don't even check Redis.
                    return false;
                }

                // If local data says that it's not yet explored, then remote data may disagree, other instances may have explored it.
                // So go out to Redis to check it.
                var couldRegisterForExploration = await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:Bundles", addressToProcessUri.AbsoluteUri);

                // Always true. If we could register it against remote data, then we have just reserved it successfully,
                // and if we could not reserve it, then another instance has already reserved it, so by setting it to true,
                // we sync the local data.
                exploredBundleIds.Set(bundleId, true);

                return couldRegisterForExploration;
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Developer, StringComparison.OrdinalIgnoreCase))
            {
                return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:Developer", addressToProcessUri.AbsoluteUri);
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Publisher, StringComparison.OrdinalIgnoreCase))
            {
                return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:Publisher", addressToProcessUri.AbsoluteUri);
            }

            // TODO: These are not yet usable, because the set difference used in the DiscoverSteamLinksAsync method relies on a single 'Explored' set.
            /*
            const int bitmapSize = 1024;

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

            return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored:EverythingElse", addressToProcessUri.AbsoluteUri);
            */

            return await redisDatabase.SetAddAsync($"Crawler:{redisKeyDateStamp}:Explored", addressToProcessUri.AbsoluteUri);
        }

        private static bool IsLinkAllowedForExploration(Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri;

            if (absoluteUri.StartsWith(PageUrlPrefixes.Publisher) || absoluteUri.StartsWith(PageUrlPrefixes.Developer))
            {
                if (uri.Segments.DefaultIfEmpty(string.Empty).Last().Trim('/') == "about")
                {
                    return false;
                }
            }

            return
                LinksAllowedForExploration.Contains(absoluteUri) ||
                LinkPrefixesAllowedForExploration.Any(x => absoluteUri.StartsWith(x));
        }
    }
}