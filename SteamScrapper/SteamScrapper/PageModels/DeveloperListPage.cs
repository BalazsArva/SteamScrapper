using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SteamScrapper.Extensions;
using SteamScrapper.Utilities;

namespace SteamScrapper.PageModels
{
    public class DeveloperListPage : SteamPage
    {
        private DeveloperListPage(Uri baseAddress, HtmlDocument htmlDocument)
            : base(baseAddress, htmlDocument)
        {
        }

        public static async Task<DeveloperListPage> CreateAsync()
        {
            const int resultsPerPage = 24;
            const string baseAddress = "https://store.steampowered.com/";

            var cookieContainer = new CookieContainer();
            using var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var client = new HttpClient(handler);

            cookieContainer.Add(new Uri(baseAddress), new Cookie("lastagecheckage", "1-0-1980"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("birthtime", "312850801"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("wants_mature_content", "1"));

            var result = await client.GetStringAsync("https://store.steampowered.com/developer/");
            var doc = new HtmlDocument();

            doc.LoadHtml(result);

            // Extract AJAX pager links
            var recommendationRows = doc.GetDescendantById("RecommendationsRows");
            var recommendationsTotal = doc.GetDescendantById("Recommendations_total");
            var totalPages = 0;

            if (int.TryParse(recommendationsTotal?.InnerText.Replace(",", "").Replace(".", ""), out var total))
            {
                totalPages = (int)Math.Ceiling(total / (double)resultsPerPage);
            }

            var resultsForAllPages = await GetAllPagesAsync(client, totalPages, resultsPerPage);

            foreach (var pageResult in resultsForAllPages)
            {
                // The results don't have a single root. Need to add an artificial wrapper around them so there is 1 root node only.
                var resultsHtmlWrapper = string.Concat("<div>", pageResult.ResultsHtml, "</div>");
                var resultsWrapperNode = HtmlNode.CreateNode(resultsHtmlWrapper);

                // Extract the original, root-less elements as a collection to add them to their desired location.
                recommendationRows.AppendChildren(resultsWrapperNode.ChildNodes);
            }

            return new DeveloperListPage(new Uri(baseAddress), doc);
        }

        private static async Task<IEnumerable<PagingResult>> GetAllPagesAsync(HttpClient client, int totalPages, int resultsPerPage)
        {
            const int parallelDownloads = 16;

            var finalResults = new List<PagingResult>(totalPages);
            var allUrlsSegmentated = Enumerable
                .Range(0, totalPages)
                .Select(page =>
                {
                    var start = page * resultsPerPage;

                    return $"https://store.steampowered.com/curators/ajaxgettopcreatorhomes/render/?start={start}&count={resultsPerPage}";
                })
                .Segmentate(parallelDownloads)
                .ToList();

            foreach (var segment in allUrlsSegmentated)
            {
                var tasks = new List<Task<PagingResult>>();
                foreach (var url in segment)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            Console.WriteLine($"Downloading from URL '{url}'...");

                            var pageResponseJson = await client.GetStringAsync(url);
                            return JsonConvert.DeserializeObject<PagingResult>(pageResponseJson);
                        }
                        catch
                        {
                            // TODO: Logging
                            Console.WriteLine($"Could not download from URL '{url}'.");

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