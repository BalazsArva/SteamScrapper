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
    public class SubPage : SteamPage
    {
        public const string UnknownSubName = "Unknown product";
        public const decimal UnknownPrice = -1;

        // TODO: Implement other stuff (which apps it contains, etc.)
        public SubPage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml, HtmlElements.HeaderLevel2, HtmlElements.Div)
        {
            if (!(address.AbsoluteUri ?? string.Empty).StartsWith(PageUrlPrefixes.Sub, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must start with '{PageUrlPrefixes.Sub}'.",
                    nameof(address));
            }

            SubId = SteamLinkHelper.ExtractSubId(address);
            IncludedAppIds = ExtractIncludedAppIds();

            // Currently, we assume it's always €
            Price = ExtractPriceInEuros();
        }

        public long SubId { get; }

        public Price Price { get; }

        public IEnumerable<long> IncludedAppIds { get; }

        protected override string ExtractFriendlyName()
        {
            var appNameDiv = PrefetchedHtmlNodes[HtmlElements.HeaderLevel2].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Class, "pageheader"));

            return appNameDiv is null ? UnknownSubName : appNameDiv.InnerText;
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
            var addToCartForm = PrefetchedHtmlNodes[HtmlElements.Form].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Name, $"add_to_cart_{SubId}"));

            if (addToCartForm is null)
            {
                discountPrice = default;
                originalPrice = default;
                return false;
            }

            var divDescendants = addToCartForm
                .ParentNode
                .GetDescendantsByNames(HtmlElements.Div)[HtmlElements.Div];

            var originalPriceText = divDescendants.FirstOrDefault(x => x.HasClass("discount_original_price"))?.InnerText ?? string.Empty;
            var discountPriceText = divDescendants.FirstOrDefault(x => x.HasClass("discount_final_price"))?.InnerText ?? string.Empty;

            var couldExtractOriginalPrice = TryGetPrice(originalPriceText, currencySymbols, out var originalPriceResult);
            var couldExtractDiscountPrice = TryGetPrice(discountPriceText, currencySymbols, out var discountPriceResult);
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
            var addToCartForm = PrefetchedHtmlNodes[HtmlElements.Form].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Name, $"add_to_cart_{SubId}"));

            if (addToCartForm is null)
            {
                originalPrice = default;
                return false;
            }

            var originalPriceText = addToCartForm
                .ParentNode
                .GetDescendantsByNames(HtmlElements.Div)[HtmlElements.Div]
                .FirstOrDefault(x => x.HasClass("game_purchase_price"))?.InnerText ?? string.Empty;

            var couldExtractOriginalPrice = TryGetPrice(originalPriceText, currencySymbols, out var originalPriceResult);
            if (couldExtractOriginalPrice)
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