using System;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Html;
using SteamScrapper.Common.Urls;
using SteamScrapper.Common.Utilities.Links;

namespace SteamScrapper.Domain.PageModels
{
    public class BundlePage : SteamPage
    {
        public const string UnknownBundleName = "Unknown bundle";

        public BundlePage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml, HtmlElements.Link, HtmlElements.Div, HtmlElements.HeaderLevel2)
        {
            if (!(address.AbsoluteUri ?? string.Empty).StartsWith(PageUrlPrefixes.Bundle, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must start with '{PageUrlPrefixes.Bundle}'.",
                    nameof(address));
            }

            BundleId = SteamLinkHelper.ExtractBundleId(address);
            Price = ExtractPrice();
            BannerUrl = ExtractBannerUrl();
        }

        public int BundleId { get; }

        public Uri BannerUrl { get; }

        public decimal Price { get; }

        protected override string ExtractFriendlyName()
        {
            var appNameDiv = PrefetchedHtmlNodes[HtmlElements.HeaderLevel2].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Class, "pageheader"));

            return appNameDiv is null ? UnknownBundleName : appNameDiv.InnerText;
        }

        private Uri ExtractBannerUrl()
        {
            var bannerImageHolderNode1 = PrefetchedHtmlNodes[HtmlElements.Div]
                .FirstOrDefault(x =>
                    x.HasAttribute(HtmlAttributes.Id, "package_header_container"));

            // This can be found in the page
            if (bannerImageHolderNode1 is not null)
            {
                var imgNode = bannerImageHolderNode1
                    .Descendants(HtmlElements.Image)
                    .FirstOrDefault(x =>
                        x.HasAttribute(HtmlAttributes.Class, "package_header") &&
                        x.HasAttribute(HtmlAttributes.Source, HtmlAttributeValueTypes.NotEmpty | HtmlAttributeValueTypes.AbsoluteUri));

                if (imgNode is not null)
                {
                    var bannerUrl = imgNode.GetAttributeValue(HtmlAttributes.Source, null);

                    return new Uri(bannerUrl, UriKind.Absolute);
                }
            }

            // This can be found in <head>'s links, but it does not always exist.
            var bannerImageHolderNode2 = PrefetchedHtmlNodes[HtmlElements.Link]
                .FirstOrDefault(x =>
                    x.HasAttribute(HtmlAttributes.Relation, HtmlAttributeValues.ImageSrc) &&
                    x.HasAttribute(HtmlAttributes.Href, HtmlAttributeValueTypes.NotEmpty | HtmlAttributeValueTypes.AbsoluteUri));

            if (bannerImageHolderNode2 is not null)
            {
                var bannerUrl = bannerImageHolderNode2.GetAttributeValue(HtmlAttributes.Href, null);

                return new Uri(bannerUrl, UriKind.Absolute);
            }

            return null;
        }

        private decimal ExtractPrice()
        {
            var addToCartForm = PrefetchedHtmlNodes[HtmlElements.Form].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Name, $"add_bundle_to_cart_{BundleId}"));

            if (addToCartForm is not null)
            {
                // Note: this currently assumes €, unaware of currencies.
                var finalPriceCents = addToCartForm
                    .ParentNode
                    .GetDescendantsByNames(HtmlElements.Div)[HtmlElements.Div]
                    .Select(div => div.GetAttributeValue("data-price-final", -1))
                    .Where(finalPriceValue => finalPriceValue != -1)
                    .DefaultIfEmpty(-1)
                    .FirstOrDefault();

                return finalPriceCents == -1 ? -1 : finalPriceCents / 100m;
            }

            return -1;
        }
    }
}