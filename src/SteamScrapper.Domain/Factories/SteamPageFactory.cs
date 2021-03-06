using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SteamScrapper.Common.Constants;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Domain.Factories
{
    public class SteamPageFactory : ISteamPageFactory
    {
        private readonly ISteamService steamService;

        public SteamPageFactory(ISteamService steamService)
        {
            this.steamService = steamService ?? throw new ArgumentNullException(nameof(steamService));
        }

        public async Task<SteamPage> CreateSteamPageAsync(Uri uri, string pageHtml)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (string.IsNullOrWhiteSpace(pageHtml))
            {
                throw new ArgumentException($"The parameter '{nameof(pageHtml)}' cannot be null, empty or whitespace-only.", nameof(pageHtml));
            }

            var absoluteUri = uri.AbsoluteUri;
            var doc = new HtmlDocument();

            doc.LoadHtml(pageHtml);

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

        public async Task<SteamPage> CreateSteamPageAsync(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var pageHtml = await steamService.GetPageHtmlAsync(uri);

            return await CreateSteamPageAsync(uri, pageHtml);
        }

        public async Task<AppPage> CreateAppPageAsync(int appId)
        {
            var uri = SteamLinkHelper.CreateAppUri(appId);
            var html = await steamService.GetPageHtmlAsync(uri);
            var doc = new HtmlDocument();

            doc.LoadHtml(html);

            return new AppPage(uri, doc);
        }

        public async Task<SubPage> CreateSubPageAsync(int subId)
        {
            var uri = SteamLinkHelper.CreateSubUri(subId);
            var html = await steamService.GetPageHtmlAsync(uri);
            var doc = new HtmlDocument();

            doc.LoadHtml(html);

            return new SubPage(uri, doc);
        }
    }
}