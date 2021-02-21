using System;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Constants;

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

            BundleId = ExtractBundleId(address);
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

        private static int ExtractBundleId(Uri address)
        {
            var segmentsWithoutSlashes = address.Segments.Select(x => x.TrimEnd('/')).ToList();

            if (segmentsWithoutSlashes.Count < 3)
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must contain the bundle Id after '{PageUrlPrefixes.Bundle}'.",
                    nameof(address));
            }

            if (!int.TryParse(segmentsWithoutSlashes[2], out var bundleId) || bundleId < 1)
            {
                throw new ArgumentException(
                   $"The provided address is invalid. The bundle Id in a valid address must be a positive integer.",
                   nameof(address));
            }

            return bundleId;
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