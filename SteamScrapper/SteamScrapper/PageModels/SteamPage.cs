using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Constants;
using SteamScrapper.PageModels.SpecialLinks;
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
            NormalizedAddress = LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(address);
            FriendlyName = ExtractFriendlyName();

            var htmlLinks = PageHtml.DocumentNode.Descendants(LinkHtmlTagName).ToList();
            var subLinks = GetLinksForSubs(PageHtml).ToList();

            NormalizedLinks = htmlLinks
                .Select(x => x.GetAttributeValue("href", null))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => x.StartsWith(PageUrlPrefixes.Steam, StringComparison.OrdinalIgnoreCase))
                .Select(x => LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(new Uri(x, UriKind.Absolute)))
                .Concat(subLinks.Select(x => x.Address))
                .Distinct(UriAbsoluteUriEqualityComparer.Instance)
                .OrderBy(x => x.AbsoluteUri)
                .ToList();

            AppLinks = NormalizedLinks
                .Where(uri => uri.AbsoluteUri.StartsWith(PageUrlPrefixes.App))
                .OrderBy(x => x.AbsoluteUri)
                .Select(x => new AppLink(x))
                .ToList();

            BundleLinks = NormalizedLinks
                .Where(uri => uri.AbsoluteUri.StartsWith(PageUrlPrefixes.Bundle))
                .OrderBy(x => x.AbsoluteUri)
                .Select(x => new BundleLink(x))
                .ToList();

            DlcLinks = NormalizedLinks
                .Where(uri => uri.AbsoluteUri.StartsWith(PageUrlPrefixes.Dlc))
                .OrderBy(x => x.AbsoluteUri)
                .ToList();

            SubLinks = subLinks;
        }

        public Uri NormalizedAddress { get; }

        public string FriendlyName { get; }

        public HtmlDocument PageHtml { get; }

        public IEnumerable<Uri> NormalizedLinks { get; }

        public IEnumerable<AppLink> AppLinks { get; }

        public IEnumerable<BundleLink> BundleLinks { get; }

        public IEnumerable<Uri> DlcLinks { get; }

        public IEnumerable<SubLink> SubLinks { get; }

        private static IEnumerable<SubLink> GetLinksForSubs(HtmlDocument pageHtml)
        {
            var addToCartForms = pageHtml
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

            var results = new List<SubLink>(addToCartForms.Count);
            for (var i = 0; i < addToCartForms.Count; ++i)
            {
                var form = addToCartForms[i];
                var subId = form
                    .Descendants("input")
                    .Where(input =>
                    {
                        var inputType = input.GetAttributeValue("type", string.Empty).Trim().ToLower();
                        var inputName = input.GetAttributeValue("name", string.Empty).Trim().ToLower();

                        return inputType == "hidden" && inputName == "subid";
                    })
                    .Select(input => input.GetAttributeValue("value", string.Empty).Trim().ToLower())
                    .Where(subId => !string.IsNullOrWhiteSpace(subId))
                    .FirstOrDefault();

                if (subId is null)
                {
                    continue;
                }

                results.Add(new SubLink(new Uri($"https://store.steampowered.com/sub/{subId}/", UriKind.Absolute)));
            }

            return results;
        }

        protected virtual string ExtractFriendlyName() => "Unknown";
    }
}