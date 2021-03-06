using System;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Common.Constants;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Utilities.Links;

namespace SteamScrapper.Domain.PageModels
{
    public class AppPage : SteamPage
    {
        public const string UnknownAppName = "Unknown product";

        // TODO: Implement other stuff (price, title extraction, etc.)
        public AppPage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml, HtmlElements.Link)
        {
            AppId = SteamLinkHelper.ExtractAppId(address);
            BannerUrl = ExtractBannerUrl();
        }

        public int AppId { get; }

        public Uri BannerUrl { get; }

        protected override string ExtractFriendlyName()
        {
            var titleContainer = PageHtml
                .FastEnumerateDescendants()
                .Where(x => x.Name == HtmlElements.Div)
                .FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Class, "apphub_AppName"));

            return titleContainer?.InnerText ?? UnknownAppName;
        }

        private Uri ExtractBannerUrl()
        {
            var bannerImageHolderNode = PrefetchedHtmlNodes[HtmlElements.Link]
                .FirstOrDefault(x =>
                    x.HasAttribute(HtmlAttributes.Relation, HtmlAttributeValues.ImageSrc) &&
                    !string.IsNullOrWhiteSpace(x.GetAttributeValue(HtmlAttributes.Href, null)));

            if (bannerImageHolderNode is not null)
            {
                var bannerUrl = bannerImageHolderNode.GetAttributeValue(HtmlAttributes.Href, null);

                if (Uri.TryCreate(bannerUrl, UriKind.Absolute, out var result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}