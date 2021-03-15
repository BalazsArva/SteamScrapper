using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Exceptions;

namespace SteamScrapper.Infrastructure.Services
{
    public class SteamService : ISteamService
    {
        public const string baseAddress = "https://store.steampowered.com/";

        public const int WebRequestRetryLimit = 15;
        public const int WebRequestRetryDelayInitialMillis = 1000;
        public const int WebRequestRetryDelayIncrementMillis = 250;

        private readonly HttpClient client;
        private readonly HttpClient redirectionFollowerClient;
        private readonly ILogger logger;

        public SteamService(ILogger<SteamService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var cookieContainer = new CookieContainer();

            cookieContainer.Add(new Uri(baseAddress), new Cookie("lastagecheckage", "1-0-1980"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("birthtime", "312850801"));
            cookieContainer.Add(new Uri(baseAddress), new Cookie("wants_mature_content", "1"));

            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = cookieContainer,
            };

            client = new HttpClient(clientHandler);
            redirectionFollowerClient = new HttpClient(new HttpClientHandler
            {
                CookieContainer = cookieContainer,
            });
        }

        public async Task<string> GetPageHtmlWithoutRetryAsync(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            HttpStatusCode statusCode;

            try
            {
                var responseMessage = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead);

                if (responseMessage.IsSuccessStatusCode)
                {
                    return await responseMessage.Content.ReadAsStringAsync();
                }

                statusCode = responseMessage.StatusCode;
            }
            catch (Exception e)
            {
                throw new SteamServiceException(uri, e);
            }

            if (statusCode == HttpStatusCode.Forbidden)
            {
                throw new SteamRateLimitExceededException(uri);
            }

            if (statusCode == HttpStatusCode.Moved ||
                statusCode == HttpStatusCode.MovedPermanently ||
                statusCode == HttpStatusCode.Redirect ||
                statusCode == HttpStatusCode.RedirectMethod ||
                statusCode == HttpStatusCode.Found)
            {
                throw new SteamPageRemovedException((int)statusCode, uri);
            }

            throw new SteamUnexpectedStatusCodeException((int)statusCode, uri);
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
                    var responseJson = await redirectionFollowerClient.GetStringAsync(uri);

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