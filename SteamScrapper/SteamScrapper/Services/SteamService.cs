using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SteamScrapper.PageModels;

namespace SteamScrapper.Services
{
    public class SteamService
    {
        public const string baseAddress = "https://store.steampowered.com/";
        public const int WebRequestRetryLimit = 10;
        public const int WebRequestRetryDelayMillis = 1000;

        private HttpClient client;

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

        public async Task<SteamPage> CreateSteamPageAsync(Uri uri)
        {
            var pageHtml = await DownloadPageHtmlAsync(uri);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageHtml);

            return new SteamPage(uri, htmlDocument);
        }
    }
}