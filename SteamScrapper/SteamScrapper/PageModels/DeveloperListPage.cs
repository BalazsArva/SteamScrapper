using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SteamScrapper.Common.Constants;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Services;

namespace SteamScrapper.PageModels
{
    public class DeveloperListPage : SteamPage
    {
        public const int ResultsPerPage = 24;
        public const int WebRequestRetryLimit = 10;
        public const int WebRequestRetryDelayMillis = 1000;

        private DeveloperListPage(Uri baseAddress, HtmlDocument htmlDocument)
            : base(baseAddress, htmlDocument)
        {
        }

        public static async Task<DeveloperListPage> CreateAsync(ISteamService steamService)
        {
            var doc = new HtmlDocument();
            var addressUri = new Uri(PageUrls.DeveloperList, UriKind.Absolute);

            var result = await steamService.GetPageHtmlAsync(addressUri);

            doc.LoadHtml(result);

            // Extract AJAX pager links
            var recommendationRows = doc.GetDescendantById("RecommendationsRows");
            var recommendationsTotal = doc.GetDescendantById("Recommendations_total");
            var totalPages = 0;

            if (int.TryParse(recommendationsTotal?.InnerText.Replace(",", "").Replace(".", ""), out var total))
            {
                totalPages = (int)Math.Ceiling(total / (double)ResultsPerPage);
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

        private static async Task<IEnumerable<PagingResult>> GetAllPagesAsync(ISteamService steamService, int totalPages)
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
                        var start = page * ResultsPerPage;
                        var address = $"https://store.steampowered.com/curators/ajaxgettopcreatorhomes/render/?start={start}&count={ResultsPerPage}";

                        try
                        {
                            Console.WriteLine($"Attempting to download result page from URL '{address}'...");

                            return await steamService.GetJsonAsync<PagingResult>(new Uri(address, UriKind.Absolute));
                        }
                        catch (Exception e)
                        {
                            // TODO: Improve Logging
                            Console.WriteLine($"Could not download from URL '{address}'.");

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