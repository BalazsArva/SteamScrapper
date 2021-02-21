using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SteamScrapper.PageModels;

namespace SteamScrapper.Services
{
    public class SteamService : ISteamService
    {
        public const string baseAddress = "https://store.steampowered.com/";
        public const int WebRequestRetryLimit = 10;
        public const int WebRequestRetryDelayMillis = 1000;

        private readonly HttpClient client;

        public SteamService()
        {
            var cookieContainer = new CookieContainer();

            cookieContainer.Add(new Uri(baseAddress), new Cookie("lastagecheckage", "1-0-1980"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("birthtime", "312850801"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("wants_mature_content", "1"));

            client = new HttpClient(new HttpClientHandler { CookieContainer = cookieContainer });
        }

        public async Task<string> DownloadPageHtmlAsync(Uri uri)
        {
            Exception exceptionThrown = null;

            for (var i = 0; i < WebRequestRetryLimit; ++i)
            {
                try
                {
                    return await client.GetStringAsync(uri);
                }
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.BadGateway || e.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    exceptionThrown = e;

                    if (i < WebRequestRetryLimit - 1)
                    {
                        await Task.Delay(WebRequestRetryDelayMillis);
                    }
                }
            }

            throw exceptionThrown;
        }

        public async Task<TResult> GetJsonAsync<TResult>(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var capturedExceptions = new List<Exception>(WebRequestRetryLimit);

            for (var i = 0; i < WebRequestRetryLimit; ++i)
            {
                try
                {
                    var responseJson = await client.GetStringAsync(uri);

                    return JsonConvert.DeserializeObject<TResult>(responseJson);
                }
                catch (Exception e)
                {
                    capturedExceptions.Add(e);

                    if (i < WebRequestRetryLimit - 1)
                    {
                        await Task.Delay(WebRequestRetryDelayMillis);
                    }
                }
            }

            throw new AggregateException(
                $"One or more errors occurred during the execution of GET JSON request to '{uri.AbsoluteUri}'.",
                capturedExceptions);
        }

        public async Task<SteamPage> CreateSteamPageAsync(Uri uri)
        {
            var pageHtml = await DownloadPageHtmlAsync(uri);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageHtml);

            return new SteamPage(uri, htmlDocument);
        }
    }
}