using System;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Common.Constants;
using SteamScrapper.Common.Utilities.Links;

namespace SteamScrapper.PageModels
{
    public class BundlePage : SteamPage
    {
        public const string UnknownBundleName = "Unknown bundle";

        public BundlePage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml)
        {
            if (!(address.AbsoluteUri ?? string.Empty).StartsWith(PageUrlPrefixes.Bundle, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must start with '{PageUrlPrefixes.Bundle}'.",
                    nameof(address));
            }

            BundleId = SteamLinkHelper.ExtractBundleId(address);
            Price = ExtractPrice();
        }

        public int BundleId { get; }

        public decimal Price { get; }

        protected override string ExtractFriendlyName()
        {
            var h2 = PageHtml.DocumentNode.Descendants("h2");
            var header = h2.FirstOrDefault(x => x.Attributes.Any(a => a.Name == "class" && a.Value == "pageheader"));

            return header is null ? UnknownBundleName : header.InnerText;
        }

        private decimal ExtractPrice()
        {
            var addToCartForm = PageHtml
                .DocumentNode
                .Descendants("form")
                .Where(form => form.GetAttributeValue("name", null) == $"add_bundle_to_cart_{BundleId}")
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