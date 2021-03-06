using System;
using System.Linq;
using System.Text.RegularExpressions;
using SteamScrapper.Common.Urls;

namespace SteamScrapper.Common.Utilities.Links
{
    public static class LinkSanitizer
    {
        public const string SteamStoreHost = "store.steampowered.com";

        private static readonly Regex MultipleSlashReplacer = new Regex("//+", RegexOptions.Compiled);

        public static Uri GetSanitizedLinkWithoutQueryAndFragment(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException($"This method only supports absolute URIs. The received URI '{uri}' is not an abosolute URI.", nameof(uri));
            }

            if (!string.Equals(uri.Host, SteamStoreHost, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"This method only supports URIs that start with '{SteamStoreHost}'. The received URI was '{uri}'.", nameof(uri));
            }

            // Sometimes links contain multiple slashes (/) consecutively. Replace those with 1 only.
            // Must do this against the concatenated segments, because the scheme (eg. https) is expected to still have multiple slashes.
            var normalizedSegments = MultipleSlashReplacer.Replace(string.Concat(uri.Segments), "/");

            var uriWithoutQueryAndFragment = new UriBuilder(uri.Scheme, uri.Host, uri.Port, normalizedSegments).Uri;
            var absoluteUri = uriWithoutQueryAndFragment.AbsoluteUri.Trim().ToLower();

            // If the link is an app link, then those are sometimes like 'https://store.steampowered.com/app/378648/The_Witcher_3_Wild_Hunt__Blood_and_Wine/',
            // other times without the title: 'https://store.steampowered.com/app/378648/'. For consistency, strip the title.
            if (absoluteUri.StartsWith(PageUrlPrefixes.App, StringComparison.OrdinalIgnoreCase))
            {
                var appId = new string(absoluteUri.Skip(PageUrlPrefixes.App.Length).TakeWhile(char.IsDigit).ToArray());
                var appUriString = string.Concat(PageUrlPrefixes.App, appId, "/");

                return new Uri(appUriString, UriKind.Absolute);
            }

            // If the link is a bundle link, then those are sometimes like 'https://store.steampowered.com/bundle/12231/Shadow_of_the_Tomb_Raider_Definitive_Edition/',
            // other times without the title: 'https://store.steampowered.com/bundle/12231/'. For consistency, strip the title.
            if (absoluteUri.StartsWith(PageUrlPrefixes.Bundle))
            {
                var bundleId = new string(absoluteUri.Skip(PageUrlPrefixes.Bundle.Length).TakeWhile(char.IsDigit).ToArray());
                var bundleUriString = string.Concat(PageUrlPrefixes.Bundle, bundleId, "/");

                return new Uri(bundleUriString, UriKind.Absolute);
            }

            // If the link is a DLCs link, then those are sometimes like 'https://store.steampowered.com/dlc/391220/Rise_of_the_Tomb_Raider/',
            // other times without the title: 'https://store.steampowered.com/dlc/391220/'. For consistency, strip the title.
            if (absoluteUri.StartsWith(PageUrlPrefixes.Dlc))
            {
                var dlcForGameId = new string(absoluteUri.Skip(PageUrlPrefixes.Dlc.Length).TakeWhile(char.IsDigit).ToArray());
                var dlcForGameUriString = string.Concat(PageUrlPrefixes.Dlc, dlcForGameId, "/");

                return new Uri(dlcForGameUriString, UriKind.Absolute);
            }

            if (!absoluteUri.EndsWith('/'))
            {
                absoluteUri += '/';
            }

            return new Uri(absoluteUri, UriKind.Absolute);
        }
    }
}