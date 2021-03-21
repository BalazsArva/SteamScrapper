using System;
using System.Globalization;
using System.Linq;
using SteamScrapper.Common.Urls;

namespace SteamScrapper.Common.Utilities.Links
{
    public static class SteamLinkHelper
    {
        public static int ExtractBundleId(Uri address)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var segmentsWithoutSlashes = address.Segments.Select(x => x.TrimEnd('/')).ToList();

            if (segmentsWithoutSlashes.Count < 3)
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must contain the bundle Id after '{PageUrlPrefixes.Bundle}'.",
                    nameof(address));
            }

            if (!int.TryParse(segmentsWithoutSlashes[2], out var bundleId) || bundleId < 1)
            {
                throw new ArgumentException(
                   $"The provided address is invalid. The bundle Id in a valid address must be a positive integer.",
                   nameof(address));
            }

            return bundleId;
        }

        public static int ExtractSubId(Uri address)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var segmentsWithoutSlashes = address.Segments.Select(x => x.TrimEnd('/')).ToList();

            if (segmentsWithoutSlashes.Count < 3)
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must contain the sub Id after '{PageUrlPrefixes.Sub}'.",
                    nameof(address));
            }

            if (!int.TryParse(segmentsWithoutSlashes[2], out var subId) || subId < 1)
            {
                throw new ArgumentException(
                   $"The provided address is invalid. The sub Id in a valid address must be a positive integer.",
                   nameof(address));
            }

            return subId;
        }

        public static int ExtractAppId(Uri address)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var segmentsWithoutSlashes = address.Segments.Select(x => x.TrimEnd('/')).ToList();

            if (segmentsWithoutSlashes.Count < 3)
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must contain the app Id after '{PageUrlPrefixes.App}'.",
                    nameof(address));
            }

            if (!int.TryParse(segmentsWithoutSlashes[2], out var appId) || appId < 1)
            {
                throw new ArgumentException(
                   $"The provided address is invalid. The app Id in a valid address must be a positive integer.",
                   nameof(address));
            }

            return appId;
        }

        public static Uri CreateAppUri(long appId)
        {
            return new Uri(PageUrlPrefixes.App + appId.ToString(CultureInfo.InvariantCulture), UriKind.Absolute);
        }

        public static Uri CreateSubUri(int subId)
        {
            return new Uri(PageUrlPrefixes.Sub + subId.ToString(CultureInfo.InvariantCulture), UriKind.Absolute);
        }

        public static Uri CreateBundleUri(int bundleId)
        {
            return new Uri(PageUrlPrefixes.Bundle + bundleId.ToString(CultureInfo.InvariantCulture), UriKind.Absolute);
        }
    }
}