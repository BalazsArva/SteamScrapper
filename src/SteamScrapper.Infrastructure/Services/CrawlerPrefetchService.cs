using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Exceptions;

namespace SteamScrapper.Infrastructure.Services
{
    // TODO: Account for interruption (app stops before all items are processed).
    // Need to try to add back any URIs that are already reserved and are here in memory.
    // Add those back to the 'to be explored' set and remove them from the 'explored' set.
    public class CrawlerPrefetchService : ICrawlerPrefetchService
    {
        private record FetchDescriptor(Uri Uri, Task<string> HtmlDownloadTask);

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

        public async Task<SteamPage> GetNextPageAsync(DateTime utcNow)
        {
            // Clean up possibly failed tasks before determining how many new ones should be launched.
            RemoveFailedPrefetchTasks();

            await PrefetchNewItemIfNeededAsync(utcNow);

            if (prefetchList.Count == 0)
            {
                return null;
            }

            // TODO: This will stall forever if every fetch task fails, e.g. due to network outage.
            // Wait until any download task is completed. It will continue immediately if there's an already fetched item.
            await Task.WhenAny(prefetchList.Select(x => x.HtmlDownloadTask));

            // Find a completed item.
            var completedTaskIndex = prefetchList.FindIndex(x => x.HtmlDownloadTask.IsCompleted);
            var completedTask = prefetchList[completedTaskIndex];

            prefetchList.RemoveAt(completedTaskIndex);

            // Add a new prefetch task to fill the place of the just removed completed fetch task.
            await PrefetchNewItemIfNeededAsync(utcNow);

            return await steamPageFactory.CreateSteamPageAsync(completedTask.Uri, completedTask.HtmlDownloadTask.Result);
        }

        private void RemoveFailedPrefetchTasks()
        {
            for (var i = prefetchList.Count - 1; i >= 0; --i)
            {
                var taskStatus = prefetchList[i].HtmlDownloadTask.Status;

                if (taskStatus == TaskStatus.Canceled || taskStatus == TaskStatus.Faulted)
                {
                    prefetchList.RemoveAt(i);
                }
            }
        }

        private async Task PrefetchNewItemIfNeededAsync(DateTime utcNow)
        {
            var requiredPrefetchCount = PrefetchCount - prefetchList.Count;

            for (var i = 0; i < requiredPrefetchCount; ++i)
            {
                var nextAddress = await crawlerAddressRegistrationService.GetNextAddressAsync(utcNow, default);
                if (nextAddress is not null)
                {
                    prefetchList.Add(new(nextAddress, PrefetchAsync(nextAddress)));
                }
                else
                {
                    break;
                }
            }
        }

        private async Task<string> PrefetchAsync(Uri address)
        {
            try
            {
                return await steamService.GetPageHtmlWithoutRetryAsync(address);
            }
            catch (SteamRateLimitExceededException)
            {
                // TODO: Put the URL back to exploration queue
                throw;
            }
        }
    }
}