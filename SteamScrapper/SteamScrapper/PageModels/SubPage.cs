using System;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Constants;
using SteamScrapper.Utilities.LinkHelpers;

namespace SteamScrapper.PageModels
{
    public class SubPage : SteamPage
    {
        public const string UnknownSubName = "Unknown product";

        public SubPage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml)
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
            var appNameDiv = PageHtml.DocumentNode.Descendants("h2").FirstOrDefault(x => x.Attributes.Any(a => a.Name == "class" && a.Value == "pageheader"));

            return appNameDiv is null ? UnknownSubName : appNameDiv.InnerText;
        }

        private decimal ExtractPrice()
        {
            var addToCartForm = PageHtml
                .DocumentNode
                .Descendants("form")
                .Where(form => form.GetAttributeValue("name", null) == $"add_to_cart_{SubId}")
                .FirstOrDefault();

            if (addToCartForm is not null)
            {
                // Note: this currently assumes €, unaware of currencies.
                var finalPriceCents = addToCartForm
                    .ParentNode
                    .Descendants("div")
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