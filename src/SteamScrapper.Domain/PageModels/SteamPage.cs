using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Common.Constants;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Utilities.EqualityComparers;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.PageModels.SpecialLinks;

namespace SteamScrapper.Domain.PageModels
{
    public class SteamPage
    {
        public SteamPage(Uri address, HtmlDocument pageHtml, params string[] prefetchHtmlElementNames)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            PageHtml = pageHtml ?? throw new ArgumentNullException(nameof(pageHtml));
            NormalizedAddress = LinkSanitizer.GetSanitizedLinkWithoutQueryAndFragment(address);
            FriendlyName = ExtractFriendlyName();

            PrefetchedHtmlNodes = PageHtml.GetDescendantsByNames(prefetchHtmlElementNames.Union(new[] { HtmlElements.Anchor, HtmlElements.Form }).ToArray());

            var htmlLinks = PrefetchedHtmlNodes[HtmlElements.Anchor];
            var subLinks = GetLinksForSubs(PrefetchedHtmlNodes[HtmlElements.Form]);

            NormalizedLinks = htmlLinks
                .Select(x => x.GetAttributeValue(HtmlAttributes.Href, null))
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

        public IReadOnlyDictionary<string, IEnumerable<HtmlNode>> PrefetchedHtmlNodes { get; }

        private static List<SubLink> GetLinksForSubs(IEnumerable<HtmlNode> formNodes)
        {
            var addToCartForms = formNodes
                .Where(form =>
                {
                    var formAction = form.GetAttributeValue(HtmlAttributes.Action, string.Empty).Trim().TrimEnd('/').ToLower();
                    var formMethod = form.GetAttributeValue(HtmlAttributes.Method, string.Empty).Trim().TrimEnd('/').ToLower();

                    return
                        formAction == "https://store.steampowered.com/cart" &&
                        formMethod == HtmlFormMethods.Post;
                })
                .ToList();

            var results = new List<SubLink>(addToCartForms.Count);
            for (var i = 0; i < addToCartForms.Count; ++i)
            {
                var form = addToCartForms[i];
                var subId = form
                    .GetDescendantsByNames(HtmlElements.Input)[HtmlElements.Input]
                    .Where(input =>
                    {
                        var inputType = input.GetAttributeValue(HtmlAttributes.Type, string.Empty).Trim().ToLower();
                        var inputName = input.GetAttributeValue(HtmlAttributes.Name, string.Empty).Trim().ToLower();

                        return inputType == HtmlInputTypes.Hidden && inputName == "subid";
                    })
                    .Select(input => input.GetAttributeValue(HtmlAttributes.Value, string.Empty).Trim().ToLower())
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

        protected virtual IEnumerable<string> PrefetchHtmlElementNames()
        {
            return new[] { HtmlElements.Anchor, HtmlElements.Form };
        }
    }
}