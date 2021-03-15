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
    public class CrawlerPrefetchService : ICrawlerPrefetchService
    {
        private record FetchResult(SteamPage Page, Exception Exception);
        private record FetchDescriptor(Uri Uri, Task<FetchResult> PageDownloadTask);

        private const int PrefetchCount = 5;

        private readonly ICrawlerAddressRegistrationService crawlerAddressRegistrationService;
        private readonly ISteamPageFactory steamPageFactory;

        private readonly List<FetchDescriptor> prefetchList = new List<FetchDescriptor>(PrefetchCount);

        public CrawlerPrefetchService(
            ICrawlerAddressRegistrationService crawlerAddressRegistrationService,
            ISteamPageFactory steamPageFactory)
        {
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

            await Task.WhenAny(prefetchList.Select(x => x.PageDownloadTask));

            // Find a completed item.
            var completedTaskIndex = prefetchList.FindIndex(x => x.PageDownloadTask.IsCompleted);
            var completedTask = prefetchList[completedTaskIndex];
            var completedTaskResult = completedTask.PageDownloadTask.Result;

            prefetchList.RemoveAt(completedTaskIndex);

            // Add a new prefetch task to fill the place of the just removed completed fetch task.
            await PrefetchNewItemsIfNeededAsync(executionDate);

            if (completedTaskResult.Exception is SteamRateLimitExceededException)
            {
                // If we had one rate limit error, then we likely have more. Undo all reservations and start fresh at the new request,
                // otherwise we'd have to wait the cooldown for each of the already failed tasks, even if the error would otherwise no
                // longer occur.
                await CancelAllReservationsAsync(executionDate);
            }

            if (completedTaskResult.Exception is not null)
            {
                throw completedTaskResult.Exception;
            }

            return completedTaskResult.Page;
        }

        public async Task CancelAllReservationsAsync(DateTime executionDate)
        {
            var uris = prefetchList.Select(x => x.Uri).ToList();

            await crawlerAddressRegistrationService.CancelReservationsAsync(executionDate, uris);

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
                var page = await steamPageFactory.CreateSteamPageAsync(address);

                return new(page, null);
            }
            catch (Exception e)
            {
                return new(null, e);
            }
        }
    }
}