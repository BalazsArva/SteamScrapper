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
            : base(address, pageHtml)
        {
            AppId = SteamLinkHelper.ExtractAppId(address);
        }

        public int AppId { get; }

        protected override string ExtractFriendlyName()
        {
            var titleContainer = PageHtml
                .FastEnumerateDescendants()
                .Where(x => x.Name == HtmlElements.Div)
                .FirstOrDefault(x => x.HasAttribute(HtmlAttributes.Class, "apphub_AppName"));

            return titleContainer?.InnerText ?? UnknownAppName;
        }
    }
}