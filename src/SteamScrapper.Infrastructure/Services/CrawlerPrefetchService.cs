using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Infrastructure.Services
{
    // TODO: Account for interruption (app stops before all items are processed).
    // Need to try to add back any URIs that are already reserved and are here in memory.
    // Add those back to the 'to be explored' set and remove them from the 'explored' set.
    public class CrawlerPrefetchService : ICrawlerPrefetchService
    {
        private record FetchResult(string PageHtml, Exception Exception);
        private record FetchDescriptor(Uri Uri, Task<FetchResult> HtmlDownloadTask);

        private const int PrefetchCount = 5;

        private readonly ISteamService steamService;
        private readonly ICrawlerAddressRegistrationService crawlerAddressRegistrationService;
        private readonly ISteamPageFactory steamPageFactory;

        private readonly List<FetchDescriptor> prefetchList = new List<FetchDescriptor>(PrefetchCount);

        public CrawlerPrefetchService(
            ISteamService steamService,
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService,
            ISteamPageFactory steamPageFactory)
        {
            this.steamService = steamService ?? throw new ArgumentNullException(nameof(steamService));
            this.crawlerAddressRegistrationService = crawlerAddressRegistrationService ?? throw new ArgumentNullException(nameof(crawlerAddressRegistrationService));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
        }

        public async Task<SteamPage> GetNextPageAsync(DateTime executionDate)
        {
            // Populate prefetch list, this is important at first run.
            await PrefetchNewItemsIfNeededAsync(executionDate);

            if (prefetchList.Count == 0)
            {
                return null;
            }

            await Task.WhenAny(prefetchList.Select(x => x.HtmlDownloadTask));

            // Find a completed item.
            var completedTaskIndex = prefetchList.FindIndex(x => x.HtmlDownloadTask.IsCompleted);
            var completedTask = prefetchList[completedTaskIndex];
            var completedTaskResult = completedTask.HtmlDownloadTask.Result;

            prefetchList.RemoveAt(completedTaskIndex);

            // Add a new prefetch task to fill the place of the just removed completed fetch task.
            await PrefetchNewItemsIfNeededAsync(executionDate);

            if (completedTaskResult.Exception is not null)
            {
                throw completedTaskResult.Exception;
            }

            return await steamPageFactory.CreateSteamPageAsync(completedTask.Uri, completedTaskResult.PageHtml);
        }

        public async Task CancelAllReservationsAsync(DateTime executionDate)
        {
            var uris = prefetchList.Select(x => x.Uri).ToList();

            await crawlerAddressRegistrationService.UndoReservationsAsync(executionDate, uris);

            prefetchList.Clear();
        }

        private async Task PrefetchNewItemsIfNeededAsync(DateTime utcNow)
        {
            var requiredPrefetchCount = PrefetchCount - prefetchList.Count;

            for (var i = 0; i < requiredPrefetchCount; ++i)
            {
                var nextAddress = await crawlerAddressRegistrationService.GetNextAddressAsync(utcNow, default);
                if (nextAddress is not null)
                {
                    prefetchList.Add(new(nextAddress, DownloadPageHtmlAsync(nextAddress)));
                }
                else
                {
                    break;
                }
            }
        }

        private async Task<FetchResult> DownloadPageHtmlAsync(Uri address)
        {
            try
            {
                var pageHtml = await steamService.GetPageHtmlWithoutRetryAsync(address);

                return new(pageHtml, null);
            }
            catch (Exception e)
            {
                return new(null, e);
            }
        }
    }
}