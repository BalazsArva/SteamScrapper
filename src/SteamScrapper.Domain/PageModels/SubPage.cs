using System;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Common.Constants;
using SteamScrapper.Common.Extensions;
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
            Price = ExtractPrice();
        }

        public int SubId { get; }

        public decimal Price { get; }

        protected override string ExtractFriendlyName()
        {
            var appNameDiv = PrefetchedHtmlNodes[HtmlElements.HeaderLevel2].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Class, "pageheader"));

            return appNameDiv is null ? UnknownSubName : appNameDiv.InnerText;
        }

        private decimal ExtractPrice()
        {
            var addToCartForm = PrefetchedHtmlNodes[HtmlElements.Form].FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Name, $"add_to_cart_{SubId}"));

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