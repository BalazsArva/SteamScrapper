using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SteamScrapper.Constants;
using SteamScrapper.PageModels;
using SteamScrapper.Services;

namespace SteamScrapper.Utilities.Factories
{
    public class SteamPageFactory : ISteamPageFactory
    {
        private readonly ISteamService steamService;

        public SteamPageFactory(ISteamService steamService)
        {
            this.steamService = steamService ?? throw new ArgumentNullException(nameof(steamService));
        }

        public async Task<SteamPage> CreateSteamPageAsync(Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri;
            var html = await steamService.GetPageHtmlAsync(uri);
            var doc = new HtmlDocument();

            doc.LoadHtml(html);

            if (string.Equals(PageUrls.DeveloperList, absoluteUri, StringComparison.OrdinalIgnoreCase))
            {
                return await DeveloperListPage.CreateAsync(steamService);
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Bundle, StringComparison.OrdinalIgnoreCase))
            {
                return new BundlePage(uri, doc);
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.Sub, StringComparison.OrdinalIgnoreCase))
            {
                return new SubPage(uri, doc);
            }

            if (absoluteUri.StartsWith(PageUrlPrefixes.App, StringComparison.OrdinalIgnoreCase))
            {
                return new AppPage(uri, doc);
            }

            return new SteamPage(uri, doc);
        }
    }
}