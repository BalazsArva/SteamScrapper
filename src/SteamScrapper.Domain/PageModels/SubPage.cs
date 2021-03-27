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
            var price = ExtractPrice("€");
            if (price != -1)
            {
                // Euro price is represented as (Euros * 100 + Cents), e.g. 49.99 => 4999, so we need to divide it by 100 to get the real one.
                return price / 100m;
            }

            return -1;
        }

        private decimal ExtractPrice(string currencySymbols)
        {
            var addToCartForm = PrefetchedHtmlNodes[HtmlElements.Form].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Name, $"add_to_cart_{SubId}"));

            if (addToCartForm is null)
            {
                return -1;
            }

            return addToCartForm
                .ParentNode
                .GetDescendantsByNames(HtmlElements.Div)[HtmlElements.Div]
                .Select(div =>
                {
                    var price = div.GetAttributeValue("data-price-final", -1);
                    var innerText = div.InnerText?.Trim() ?? string.Empty;

                    if (innerText.EndsWith(currencySymbols))
                    {
                        return price;
                    }

                    return -1;
                })
                .Where(finalPriceValue => finalPriceValue != -1)
                .DefaultIfEmpty(-1)
                .FirstOrDefault();
        }
    }
}