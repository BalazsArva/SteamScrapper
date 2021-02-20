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
        public const int ResultsPerPage = 24;
        public const int WebRequestRetryLimit = 10;
        public const int WebRequestRetryDelayMillis = 1000;

        private DeveloperListPage(Uri baseAddress, HtmlDocument htmlDocument)
            : base(baseAddress, htmlDocument)
        {
        }

        public static async Task<DeveloperListPage> CreateAsync()
        {
            const string baseAddress = "https://store.steampowered.com/";
            const string pageAddress = "https://store.steampowered.com/developer/";

            var cookieContainer = new CookieContainer();
            using var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var client = new HttpClient(handler);

            cookieContainer.Add(new Uri(baseAddress), new Cookie("lastagecheckage", "1-0-1980"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("birthtime", "312850801"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("wants_mature_content", "1"));

            var result = await client.GetStringAsync(pageAddress);
            var doc = new HtmlDocument();

            doc.LoadHtml(result);

            // Extract AJAX pager links
            var recommendationRows = doc.GetDescendantById("RecommendationsRows");
            var recommendationsTotal = doc.GetDescendantById("Recommendations_total");
            var totalPages = 0;

            if (int.TryParse(recommendationsTotal?.InnerText.Replace(",", "").Replace(".", ""), out var total))
            {
                totalPages = (int)Math.Ceiling(total / (double)ResultsPerPage);
            }

            foreach (var pageResult in await GetAllPagesAsync(client, totalPages))
            {
                // The results don't have a single root. Need to add an artificial wrapper around them so there is 1 root node only.
                var resultsHtmlWrapper = string.Concat("<div>", pageResult.ResultsHtml, "</div>");
                var resultsWrapperNode = HtmlNode.CreateNode(resultsHtmlWrapper);

                // Extract the original, root-less elements as a collection to add them to their desired location.
                recommendationRows.AppendChildren(resultsWrapperNode.ChildNodes);
            }

            return new DeveloperListPage(new Uri(pageAddress, UriKind.Absolute), doc);
        }

        private static async Task<IEnumerable<PagingResult>> GetAllPagesAsync(HttpClient client, int totalPages)
        {
            const int parallelDownloads = 16;

            var finalResults = new List<PagingResult>(totalPages);
            var allPagesSegmentated = Enumerable
                .Range(0, totalPages)
                .Segmentate(parallelDownloads)
                .ToList();

            foreach (var segment in allPagesSegmentated)
            {
                var tasks = new List<Task<PagingResult>>();
                foreach (var page in segment)
                {
                    tasks.Add(Task.Run(async () => await DownloadPageWithRetryAsync(client, page)));
                }

                var results = await Task.WhenAll(tasks);

                finalResults.AddRange(results.Where(x => x is not null));
            }

            return finalResults;
        }

        private static async Task<PagingResult> DownloadPageWithRetryAsync(HttpClient client, int page)
        {
            var start = page * ResultsPerPage;
            var address = $"https://store.steampowered.com/curators/ajaxgettopcreatorhomes/render/?start={start}&count={ResultsPerPage}";

            // TODO: Extract retry to a helper, or use Polly
            for (var i = 0; i < WebRequestRetryLimit; ++i)
            {
                Console.WriteLine($"Attempt #{i + 1} for downloading from URL '{address}'...");

                try
                {
                    var pageResponseJson = await client.GetStringAsync(address);

                    return JsonConvert.DeserializeObject<PagingResult>(pageResponseJson);
                }
                catch (HttpRequestException e) when (
                    e.StatusCode == HttpStatusCode.Forbidden ||
                    e.StatusCode == HttpStatusCode.BadGateway ||
                    e.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    Console.WriteLine($"Could not download from URL '{address}'.");

                    // TODO: Improve Logging
                    if (i < WebRequestRetryLimit - 1)
                    {
                        await Task.Delay(WebRequestRetryDelayMillis);
                    }
                }
            }

            return null;
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