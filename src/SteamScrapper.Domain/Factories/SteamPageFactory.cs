using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Urls;
using SteamScrapper.Common.Utilities.Links;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Domain.Factories
{
    public class SteamPageFactory : ISteamPageFactory
    {
        public const int DeveloperListResultsPerPage = 24;

        private readonly ISteamService steamService;
        private readonly ILogger logger;

        public SteamPageFactory(ISteamService steamService, ILogger<SteamPageFactory> logger)
        {
            this.steamService = steamService ?? throw new ArgumentNullException(nameof(steamService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SteamPage> CreateSteamPageAsync(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var absoluteUri = uri.AbsoluteUri;

            if (string.Equals(PageUrls.DeveloperList, absoluteUri, StringComparison.OrdinalIgnoreCase))
            {
                return await CreateDeveloperListPageAsync();
            }

            var pageHtml = await steamService.GetHtmlAsync(uri);
            var doc = new HtmlDocument();

            doc.LoadHtml(pageHtml);

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

        public async Task<AppPage> CreateAppPageAsync(int appId)
        {
            var uri = SteamLinkHelper.CreateAppUri(appId);
            var html = await steamService.GetHtmlAsync(uri);
            var doc = new HtmlDocument();

            doc.LoadHtml(html);

            return new AppPage(uri, doc);
        }

        public async Task<SubPage> CreateSubPageAsync(int subId)
        {
            var uri = SteamLinkHelper.CreateSubUri(subId);
            var html = await steamService.GetHtmlAsync(uri);
            var doc = new HtmlDocument();

            doc.LoadHtml(html);

            return new SubPage(uri, doc);
        }

        public async Task<BundlePage> CreateBundlePageAsync(int bundleId)
        {
            var uri = SteamLinkHelper.CreateBundleUri(bundleId);
            var html = await steamService.GetHtmlAsync(uri);
            var doc = new HtmlDocument();

            doc.LoadHtml(html);

            return new BundlePage(uri, doc);
        }

        private async Task<DeveloperListPage> CreateDeveloperListPageAsync()
        {
            var doc = new HtmlDocument();
            var addressUri = new Uri(PageUrls.DeveloperList, UriKind.Absolute);

            var result = await steamService.GetHtmlAsync(addressUri);

            doc.LoadHtml(result);

            // Extract AJAX pager links
            var recommendationRows = doc.GetDescendantById("RecommendationsRows");
            var recommendationsTotal = doc.GetDescendantById("Recommendations_total");
            var totalPages = 0;

            if (int.TryParse(recommendationsTotal?.InnerText.Replace(",", "").Replace(".", ""), out var total))
            {
                totalPages = (int)Math.Ceiling(total / (double)DeveloperListResultsPerPage);
            }

            foreach (var pageResult in await GetAllPagesAsync(steamService, totalPages))
            {
                // The results don't have a single root. Need to add an artificial wrapper around them so there is 1 root node only.
                var resultsHtmlWrapper = string.Concat("<div>", pageResult.ResultsHtml, "</div>");
                var resultsWrapperNode = HtmlNode.CreateNode(resultsHtmlWrapper);

                // Extract the original, root-less elements as a collection to add them to their desired location.
                recommendationRows.AppendChildren(resultsWrapperNode.ChildNodes);
            }

            return new DeveloperListPage(addressUri, doc);
        }

        private async Task<IEnumerable<PagingResult>> GetAllPagesAsync(ISteamService steamService, int totalPages)
        {
            const int parallelDownloads = 16;

            var finalResults = new List<PagingResult>(totalPages);
            var segmentedPages = Enumerable
                .Range(0, totalPages)
                .Segmentate(parallelDownloads)
                .ToList();

            foreach (var segment in segmentedPages)
            {
                var tasks = new List<Task<PagingResult>>();
                foreach (var page in segment)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var start = page * DeveloperListResultsPerPage;
                        var address = $"https://store.steampowered.com/curators/ajaxgettopcreatorhomes/render/?start={start}&count={DeveloperListResultsPerPage}";

                        try
                        {
                            logger.LogInformation("Attempting to download result page from address '{@Uri}'...", address);

                            return await steamService.GetJsonAsync<PagingResult>(new Uri(address, UriKind.Absolute));
                        }
                        catch (Exception e)
                        {
                            logger.LogWarning(e, "Could not download from address '{@Uri}'.", address);

                            return null;
                        }
                    }));
                }

                var results = await Task.WhenAll(tasks);

                finalResults.AddRange(results.Where(x => x is not null));
            }

            return finalResults;
        }

        private class PagingResult
        {
            [JsonProperty("success")]
            public int Success { get; set; }

            [JsonProperty("pagesize")]
            public int PageSize { get; set; }

            [JsonProperty("total_count")]
            public int TotalCount { get; set; }

            [JsonProperty("start")]
            public int Start { get; set; }

            [JsonProperty("results_html")]
            public string ResultsHtml { get; set; }
        }
    }
}