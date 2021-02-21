using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SteamScrapper.PageModels;
using SteamScrapper.Services;

namespace SteamScrapper.Factories
{
    public class SteamPageFactory : ISteamPageFactory
    {
        public const string DeveloperListPageAddress = "https://store.steampowered.com/developer/";
        public const string BundlePagePrefix = "https://store.steampowered.com/bundle/";

        private readonly ISteamService steamService;

        public SteamPageFactory(ISteamService steamService)
        {
            this.steamService = steamService ?? throw new ArgumentNullException(nameof(steamService));
        }

        public async Task<SteamPage> CreateSteamPageAsync(Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri;

            if (string.Equals(DeveloperListPageAddress, absoluteUri, StringComparison.OrdinalIgnoreCase))
            {
                return await DeveloperListPage.CreateAsync(steamService);
            }

            if (absoluteUri.StartsWith(BundlePagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var html = await steamService.DownloadPageHtmlAsync(uri);
                var doc = new HtmlDocument();

                doc.LoadHtml(html);
                return new BundlePage(uri, doc);
            }

            // TODO: Account for other special types of pages.
            return await SteamPage.CreateAsync(absoluteUri);
        }
    }
}