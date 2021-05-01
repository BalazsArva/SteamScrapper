using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Html;
using SteamScrapper.Common.Urls;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.Models;

namespace SteamScrapper.Domain.PageModels
{
    public class BundlePage : SteamPage
    {
        public const string UnknownBundleName = "Unknown bundle";

        // TODO: Implement other stuff (which apps it contains, etc.)
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
            IncludedAppIds = ExtractIncludedAppIds();
            BannerUrl = ExtractBannerUrl();

            // Currently, we assume it's always €
            Price = ExtractPriceInEuros();
        }

        public long BundleId { get; }

        public Uri BannerUrl { get; }

        public Price Price { get; }

        public IEnumerable<long> IncludedAppIds { get; }

        protected override string ExtractFriendlyName()
        {
            var appNameDiv = PrefetchedHtmlNodes[HtmlElements.HeaderLevel2].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Class, "pageheader"));

            return appNameDiv is null ? UnknownBundleName : appNameDiv.InnerText;
        }

        private IEnumerable<long> ExtractIncludedAppIds()
        {
            const string appIdAttributeName = "data-ds-appid";

            return PrefetchedHtmlNodes[HtmlElements.Div]
                .Where(div => div.HasClass("tab_item"))
                .Where(div => div.HasAttribute(appIdAttributeName, HtmlAttributeValueTypes.Long))
                .Select(div => long.Parse(div.GetAttributeValue(appIdAttributeName, "")))
                .ToHashSet();
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

        private Price ExtractPriceInEuros()
        {
            const string EuroCurrencySymbol = "€";

            if (TryExtractDiscountPrice(EuroCurrencySymbol, out var discountPrice, out var originalPrice))
            {
                return new(originalPrice, discountPrice, EuroCurrencySymbol);
            }

            if (TryExtractNormalPrice(EuroCurrencySymbol, out var normalPrice))
            {
                return new(normalPrice, null, EuroCurrencySymbol);
            }

            return Price.Unknown;
        }

        private bool TryExtractDiscountPrice(string currencySymbols, out decimal discountPrice, out decimal originalPrice)
        {
            var addToCartSection = PrefetchedHtmlNodes[HtmlElements.Div].FirstOrDefault(x => x.HasClass("game_purchase_action"));

            if (addToCartSection is null)
            {
                discountPrice = default;
                originalPrice = default;
                return false;
            }

            var originalPriceHolder = addToCartSection
                .Descendants(HtmlElements.Div)
                .FirstOrDefault(x => x.HasClass("discount_original_price"));

            var discountPriceHolder = addToCartSection
                .Descendants(HtmlElements.Div)
                .FirstOrDefault(x => x.HasClass("discount_final_price"));

            if (originalPriceHolder is null || discountPriceHolder is null)
            {
                discountPrice = default;
                originalPrice = default;
                return false;
            }

            var couldExtractOriginalPrice = TryGetPrice(originalPriceHolder.InnerText, currencySymbols, out var originalPriceResult);
            var couldExtractDiscountPrice = TryGetPrice(discountPriceHolder.InnerText, currencySymbols, out var discountPriceResult);
            if (couldExtractOriginalPrice && couldExtractDiscountPrice)
            {
                discountPrice = discountPriceResult;
                originalPrice = originalPriceResult;
                return true;
            }

            discountPrice = default;
            originalPrice = default;
            return false;
        }

        private bool TryExtractNormalPrice(string currencySymbols, out decimal originalPrice)
        {
            var addToCartSection = PrefetchedHtmlNodes[HtmlElements.Div].FirstOrDefault(x => x.HasClass("game_purchase_action"));

            if (addToCartSection is null)
            {
                originalPrice = default;
                return false;
            }

            var discountPricesDiv = addToCartSection
                .Descendants(HtmlElements.Div)
                .FirstOrDefault(x => x.HasClass("discount_final_price"));

            if (discountPricesDiv is null)
            {
                originalPrice = default;
                return false;
            }

            var priceHolder = discountPricesDiv
                .Descendants()
                .FirstOrDefault(x => x.InnerText.Trim().EndsWith(currencySymbols));

            if (priceHolder is null)
            {
                originalPrice = default;
                return false;
            }

            if (TryGetPrice(priceHolder.InnerText, currencySymbols, out var originalPriceResult))
            {
                originalPrice = originalPriceResult;
                return true;
            }

            originalPrice = default;
            return false;
        }

        private static bool TryGetPrice(string htmlText, string currencySymbol, out decimal price)
        {
            htmlText = htmlText.Trim();

            if (htmlText.EndsWith(currencySymbol))
            {
                var priceStringWithoutCurrency = htmlText.Substring(0, htmlText.Length - currencySymbol.Length).Trim();

                // Remove any decimals and separators. The result will be 100 times the actual value.
                // Note: what happens for currencies that don't have fractionals, e.g. HUF?
                priceStringWithoutCurrency = priceStringWithoutCurrency.Replace(".", "").Replace(",", "");

                if (decimal.TryParse(priceStringWithoutCurrency, out var priceResult))
                {
                    // Euro price is represented as (Euros * 100 + Cents), e.g. 49.99 => 4999, so we need to divide it by 100 to get the real one.
                    price = priceResult / 100m;
                    return true;
                }
            }

            price = default;
            return false;
        }
    }
}