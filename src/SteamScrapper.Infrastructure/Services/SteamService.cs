using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Infrastructure.Services
{
    public class SteamService : ISteamService
    {
        public const string baseAddress = "https://store.steampowered.com/";
        public const int WebRequestRetryLimit = 15;
        public const int WebRequestRetryDelayInitialMillis = 1000;
        public const int WebRequestRetryDelayIncrementMillis = 250;

        private readonly HttpClient client;
        private readonly ILogger<SteamService> logger;

        public SteamService(ILogger<SteamService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var cookieContainer = new CookieContainer();

            cookieContainer.Add(new Uri(baseAddress), new Cookie("lastagecheckage", "1-0-1980"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("birthtime", "312850801"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("wants_mature_content", "1"));

            client = new HttpClient(new HttpClientHandler { CookieContainer = cookieContainer });
        }

        public async Task<string> GetPageHtmlAsync(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var capturedExceptions = new List<Exception>(WebRequestRetryLimit);
            var delay = WebRequestRetryDelayInitialMillis;

            for (var i = 0; i < WebRequestRetryLimit; ++i)
            {
                try
                {
                    return await client.GetStringAsync(uri);
                }
                catch (Exception e)
                {
                    capturedExceptions.Add(e);

                    if (i < WebRequestRetryLimit - 1)
                    {
                        logger.LogWarning(e, "An error occurred while trying to download HTML content from address '{@Uri}'.", uri.AbsoluteUri);

                        await Task.Delay(delay);

                        delay += WebRequestRetryDelayIncrementMillis;
                    }
                }
            }

            throw new AggregateException(
                $"One or more errors occurred during the execution of GET page HTML request to '{uri.AbsoluteUri}'.",
                capturedExceptions);
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
                        logger.LogWarning(e, "An error occurred while trying to download JSON content from address '{@Uri}'.", uri.AbsoluteUri);

                        await Task.Delay(WebRequestRetryDelayInitialMillis);
                    }
                }
            }

            throw new AggregateException(
                $"One or more errors occurred during the execution of GET JSON request to '{uri.AbsoluteUri}'.",
                capturedExceptions);
        }
    }
}