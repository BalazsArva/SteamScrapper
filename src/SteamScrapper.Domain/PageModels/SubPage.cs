using System;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Html;
using SteamScrapper.Common.Urls;
using SteamScrapper.Common.Utilities.Links;

namespace SteamScrapper.Domain.PageModels
{
    public class SubPage : SteamPage
    {
        public const string UnknownSubName = "Unknown product";
        public const decimal UnknownPrice = -1;

        public SubPage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml, HtmlElements.HeaderLevel2)
        {
            if (!(address.AbsoluteUri ?? string.Empty).StartsWith(PageUrlPrefixes.Sub, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must start with '{PageUrlPrefixes.Sub}'.",
                    nameof(address));
            }

            SubId = SteamLinkHelper.ExtractSubId(address);
            PriceInEuros = ExtractPriceInEuros();
        }

        public long SubId { get; }

        public decimal PriceInEuros { get; }

        protected override string ExtractFriendlyName()
        {
            var appNameDiv = PrefetchedHtmlNodes[HtmlElements.HeaderLevel2].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Class, "pageheader"));

            return appNameDiv is null ? UnknownSubName : appNameDiv.InnerText;
        }

        private decimal ExtractPriceInEuros()
        {
            if (TryExtractDiscountPrice("€", out var discountPrice))
            {
                // Euro price is represented as (Euros * 100 + Cents), e.g. 49.99 => 4999, so we need to divide it by 100 to get the real one.
                return discountPrice / 100m;
            }

            if (TryExtractPrice("€", out var price))
            {
                // Euro price is represented as (Euros * 100 + Cents), e.g. 49.99 => 4999, so we need to divide it by 100 to get the real one.
                return price / 100m;
            }

            return UnknownPrice;
        }

        private bool TryExtractPrice(string currencySymbols, out decimal price)
        {
            var addToCartForm = PrefetchedHtmlNodes[HtmlElements.Form].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Name, $"add_to_cart_{SubId}"));

            if (addToCartForm is null)
            {
                price = default;
                return false;
            }

            var result = addToCartForm
                .ParentNode
                .GetDescendantsByNames(HtmlElements.Div)[HtmlElements.Div]
                .Select(div =>
                {
                    var priceString = div.GetAttributeValue("data-price-final", string.Empty);
                    var price = UnknownPrice;

                    if (!decimal.TryParse(priceString, out price))
                    {
                        return UnknownPrice;
                    }

                    var innerText = div.InnerText?.Trim() ?? string.Empty;
                    if (innerText.EndsWith(currencySymbols))
                    {
                        return price;
                    }

                    return UnknownPrice;
                })
                .Where(finalPriceValue => finalPriceValue != UnknownPrice)
                .DefaultIfEmpty(UnknownPrice)
                .FirstOrDefault();

            if (result == UnknownPrice)
            {
                price = default;
                return false;
            }

            price = result;
            return true;
        }

        private bool TryExtractDiscountPrice(string currencySymbols, out decimal discountPrice)
        {
            var addToCartForm = PrefetchedHtmlNodes[HtmlElements.Form].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Name, $"add_to_cart_{SubId}"));

            if (addToCartForm is null)
            {
                discountPrice = default;
                return false;
            }

            var result = addToCartForm
                .ParentNode
                .GetDescendantsByNames(HtmlElements.Div)[HtmlElements.Div]
                .Select(div =>
                {
                    var priceString = div.GetAttributeValue("data-price-final", string.Empty);
                    var price = UnknownPrice;

                    if (!decimal.TryParse(priceString, out price))
                    {
                        return UnknownPrice;
                    }

                    var discountPriceText = div
                        .Descendants()
                        .Where(x => x.HasAttribute(HtmlAttributes.Class, "discount_final_price"))
                        .Select(x => x.InnerText.Trim())
                        .DefaultIfEmpty(string.Empty)
                        .First();

                    if (discountPriceText.EndsWith(currencySymbols))
                    {
                        return price;
                    }

                    return UnknownPrice;
                })
                .Where(finalPriceValue => finalPriceValue != UnknownPrice)
                .DefaultIfEmpty(UnknownPrice)
                .First();

            if (result == UnknownPrice)
            {
                discountPrice = default;
                return false;
            }

            discountPrice = result;
            return true;
        }
    }
}