using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamScrapper.Common.Extensions;
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

        private bool TryGetRateLimitExceededException(out SteamRateLimitExceededException steamRateLimitExceededException)
        {
            for (var i = 0; i < prefetchList.Count; ++i)
            {
                var prefetchTask = prefetchList[i].HtmlDownloadTask;

                if (prefetchTask.Status == TaskStatus.Faulted)
                {
                    var unwrappedExceptions = UnwrapAggregateException(prefetchTask.Exception);

                    var result = unwrappedExceptions.FirstOrDefault(x => x is SteamRateLimitExceededException);
                    if (result is not null)
                    {
                        steamRateLimitExceededException = (SteamRateLimitExceededException)result;
                        return true;
                    }
                }
            }

            steamRateLimitExceededException = default;
            return false;
        }

        private IEnumerable<Exception> UnwrapAggregateException(AggregateException aggregateException)
        {
            var result = new List<Exception>();
            var processingQueue = new Queue<Exception>(50);

            processingQueue.Enqueue(aggregateException);

            while (processingQueue.Count > 0)
            {
                var nextException = processingQueue.Dequeue();

                if (nextException is AggregateException nextAggregateException)
                {
                    processingQueue.EnqueueRange(nextAggregateException.InnerExceptions);
                }
                else
                {
                    result.Add(nextException);
                }
            }

            return result;
        }

        private async Task RemoveFailedPrefetchTasksAsync(DateTime executionDate)
        {
            var unreserveUris = new List<Uri>(prefetchList.Count);

            for (var i = prefetchList.Count - 1; i >= 0; --i)
            {
                var item = prefetchList[i];
                var task = item.HtmlDownloadTask;
                var taskStatus = task.Status;

                if (taskStatus == TaskStatus.Canceled)
                {
                    prefetchList.RemoveAt(i);
                }
                else if (taskStatus == TaskStatus.Faulted)
                {
                    var unwrappedException = UnwrapAggregateException(task.Exception);

                    if (unwrappedException.All(x => x is not SteamPageRemovedException))
                    {
                        // The error does not indicate that the page is removed, so it can be unreserved and retried later.
                        unreserveUris.Add(item.Uri);
                    }

                    prefetchList.RemoveAt(i);
                }
            }

            await crawlerAddressRegistrationService.UndoReservationsAsync(executionDate, unreserveUris);
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