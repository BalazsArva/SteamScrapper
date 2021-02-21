using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SteamScrapper.Constants;
using SteamScrapper.Utilities;
using SteamScrapper.Utilities.EqualityComparers;

namespace SteamScrapper.PageModels
{
    public class SteamPage
    {
        public const string LinkHtmlTagName = "a";

        public SteamPage(Uri address, HtmlDocument pageHtml)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            PageHtml = pageHtml ?? throw new ArgumentNullException(nameof(pageHtml));
            NormalizedAddress = GetNormalizedAddress(address);
            FriendlyName = ExtractFriendlyName();
            HtmlLinks = PageHtml.DocumentNode.Descendants(LinkHtmlTagName).ToList();
            NormalizedLinks = HtmlLinks
                .Select(x => x.GetAttributeValue("href", null))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => x.StartsWith(PageUrlPrefixes.Steam, StringComparison.OrdinalIgnoreCase))
                .Select(x => LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(new Uri(x, UriKind.Absolute)))
                .Distinct(UriAbsoluteUriEqualityComparer.Instance)
                .OrderBy(x => x.AbsoluteUri)
                .ToList();

            AppLinks = NormalizedLinks
                .Where(uri => uri.AbsoluteUri.StartsWith(PageUrlPrefixes.App))
                .OrderBy(x => x.AbsoluteUri)
                .ToList();

            BundleLinks = NormalizedLinks
                .Where(uri => uri.AbsoluteUri.StartsWith(PageUrlPrefixes.Bundle))
                .OrderBy(x => x.AbsoluteUri)
                .ToList();

            DlcLinks = NormalizedLinks
                .Where(uri => uri.AbsoluteUri.StartsWith(PageUrlPrefixes.Dlc))
                .OrderBy(x => x.AbsoluteUri)
                .ToList();
        }

        public Uri NormalizedAddress { get; }

        public string FriendlyName { get; }

        public HtmlDocument PageHtml { get; }

        public IEnumerable<HtmlNode> HtmlLinks { get; }

        public IEnumerable<Uri> NormalizedLinks { get; }

        public IEnumerable<Uri> AppLinks { get; }

        public IEnumerable<Uri> BundleLinks { get; }

        public IEnumerable<Uri> DlcLinks { get; }

        public IEnumerable<Uri> GetLinksForSubs()
        {
            var addToCartForms = PageHtml
                .DocumentNode
                .Descendants("form")
                .Where(form =>
                {
                    var formAction = form.GetAttributeValue("action", string.Empty).Trim().TrimEnd('/').ToLower();
                    var formMethod = form.GetAttributeValue("method", string.Empty).Trim().TrimEnd('/').ToLower();

                    return
                        formAction == "https://store.steampowered.com/cart" &&
                        formMethod == "post";
                })
                .ToList();

            var results = new List<Uri>(addToCartForms.Count);
            for (var i = 0; i < addToCartForms.Count; ++i)
            {
                var form = addToCartForms[i];
                var subId = form
                    .Descendants("input")
                    .Where(input =>
                    {
                        var inputType = input.GetAttributeValue("type", string.Empty).Trim().ToLower();
                        var inputName = input.GetAttributeValue("name", string.Empty).Trim().ToLower();

                        return
                            inputType == "hidden" &&
                            inputName == "subid";
                    })
                    .Select(input => input.GetAttributeValue("value", string.Empty).Trim().ToLower())
                    .Where(subId => !string.IsNullOrWhiteSpace(subId))
                    .FirstOrDefault();

                if (subId is null)
                {
                    continue;
                }

                results.Add(new Uri($"https://store.steampowered.com/sub/{subId}/", UriKind.Absolute));
            }

            return results;
        }

        public IEnumerable<Uri> GetLinksStartingWith(string linkPrefix)
        {
            foreach (var node in HtmlLinks.Where(x => x.Attributes.Count > 0))
            {
                var hrefAttribute = node.Attributes.FirstOrDefault(a => a.Name == "href" && a.Value.StartsWith(linkPrefix));
                if (hrefAttribute is not null)
                {
                    yield return new Uri(hrefAttribute.Value, UriKind.Absolute);
                }
            }
        }

        public static async Task<SteamPage> CreateAsync(string address)
        {
            const string baseAddress = "https://store.steampowered.com/";

            var cookieContainer = new CookieContainer();
            using var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var client = new HttpClient(handler);

            cookieContainer.Add(new Uri(baseAddress), new Cookie("lastagecheckage", "1-0-1980"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("birthtime", "312850801"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("wants_mature_content", "1"));

            var result = await client.GetStringAsync(address);
            var doc = new HtmlDocument();

            doc.LoadHtml(result);

            return new SteamPage(new Uri(address), doc);
        }

        // TODO: This shouldn't be in the base class, because the pattern below applies to /app pages only.
        protected Uri GetNormalizedAddress(Uri uri)
        {
            var segments = uri.Segments;

            // Apps links can be the following format:
            // - https://store.steampowered.com/app/292030/
            // - https://store.steampowered.com/app/292030/The_Witcher_3_Wild_Hunt/
            // For these, the first segments will be:
            // - /
            // - app/
            // - <id>/
            // - [OPTIONALLY] The_Witcher_3_Wild_Hunt/
            // We want to detect the links which refer to the same, so we drop the game name and make use of the Id only.
            var normalizedSegments = string.Concat(segments.Skip(1).Take(2));
            var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, normalizedSegments);

            return builder.Uri;
        }

        protected virtual string ExtractFriendlyName() => "Unknown";
    }
}