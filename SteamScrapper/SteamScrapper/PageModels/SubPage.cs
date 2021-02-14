using System;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SteamScrapper.PageModels
{
    public class SubPage : SteamPage
    {
        public const string UnknownSubName = "Unknown product";
        public const string PageTypePrefix = "https://store.steampowered.com/sub/";

        private static readonly HtmlWeb Downloader = new HtmlWeb();

        public SubPage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml)
        {
            if (!(address.AbsoluteUri ?? string.Empty).StartsWith(PageTypePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must start with '{PageTypePrefix}'.",
                    nameof(address));
            }

            SubId = ExtractSubId(address);
            Price = ExtractPrice();
        }

        public int SubId { get; }

        public decimal Price { get; }

        public static async Task<SubPage> CreateAsync(string address)
        {
            var doc = await Downloader.LoadFromWebAsync(address);

            return new SubPage(new Uri(address), doc);
        }

        public static async Task<SubPage> CreateAsync(int subId)
        {
            return await CreateAsync($"{PageTypePrefix}{subId}");
        }

        protected override string ExtractFriendlyName()
        {
            var appNameDiv = PageHtml.DocumentNode.Descendants("h2").FirstOrDefault(x => x.Attributes.Any(a => a.Name == "class" && a.Value == "pageheader"));

            return appNameDiv is null ? UnknownSubName : appNameDiv.InnerText;
        }

        private static int ExtractSubId(Uri address)
        {
            var segmentsWithoutSlashes = address.Segments.Select(x => x.TrimEnd('/')).ToList();

            if (segmentsWithoutSlashes.Count < 3)
            {
                throw new ArgumentException(
                    $"The provided address is invalid. Valid addresses must contain the sub Id after '{PageTypePrefix}'.",
                    nameof(address));
            }

            if (!int.TryParse(segmentsWithoutSlashes[2], out var subId) || subId < 1)
            {
                throw new ArgumentException(
                   $"The provided address is invalid. The sub Id in a valid address must be a positive integer.",
                   nameof(address));
            }

            return subId;
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