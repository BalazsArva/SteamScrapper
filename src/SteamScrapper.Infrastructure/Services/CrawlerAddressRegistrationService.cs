using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Infrastructure.Services
{
    public class CrawlerAddressRegistrationService : ICrawlerAddressRegistrationService
    {
        private readonly IDatabase redisDatabase;

        public CrawlerAddressRegistrationService(IConnectionMultiplexer connectionMultiplexer)
        {
            if (connectionMultiplexer is null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }

            redisDatabase = connectionMultiplexer.GetDatabase();
        }

        public async Task<Uri> GetNextAddressAsync(DateTime executionDate)
        {
            var redisKeyDateStamp = executionDate.ToString("yyyyMMdd");

            while (true)
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
    }
}