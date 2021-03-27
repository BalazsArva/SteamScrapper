using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Repositories.Models;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Exceptions;
using SteamScrapper.SubScanner.Options;

namespace SteamScrapper.SubScanner.Commands.ScanSubBatch
{
    public class ScanSubBatchCommandHandler : IScanSubBatchCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ISubScanningService subScanningService;
        private readonly ISubQueryRepository subQueryRepository;
        private readonly ISubWriteRepository subWriteRepository;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        private readonly int degreeOfParallelism = 8;

        public ScanSubBatchCommandHandler(
            IDateTimeProvider dateTimeProvider,
            ISubScanningService subScanningService,
            ISubQueryRepository subQueryRepository,
            ISubWriteRepository subWriteRepository,
            IOptions<ScanSubBatchOptions> options,
            ISteamPageFactory steamPageFactory,
            ILogger<ScanSubBatchCommandHandler> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Value is null)
            {
                throw new ArgumentException(
                    "The provided configuration object does not contain valid settings for sub batch processing.",
                    nameof(options));
            }

            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.subScanningService = subScanningService ?? throw new ArgumentNullException(nameof(subScanningService));
            this.subQueryRepository = subQueryRepository ?? throw new ArgumentNullException(nameof(subQueryRepository));
            this.subWriteRepository = subWriteRepository ?? throw new ArgumentNullException(nameof(subWriteRepository));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            degreeOfParallelism = options.Value.DegreeOfParallelism;

            logger.LogInformation("Using degree of parallelism: {@DegreeOfParallelism}", degreeOfParallelism);
        }

        public async Task<ScanSubBatchCommandResult> ScanSubBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var utcNow = dateTimeProvider.UtcNow;

            var subIdsToScan = await subScanningService.GetNextSubIdsForScanningAsync(utcNow);
            if (!subIdsToScan.Any())
            {
                logger.LogInformation("Could not find more subs to scan.");
                return ScanSubBatchCommandResult.NoMoreItems;
            }

            var subIdsSegments = subIdsToScan.Segmentate(degreeOfParallelism);
            foreach (var subIdsSegment in subIdsSegments)
            {
                await ProcessSubIdsAsync(subIdsSegment);
            }

            var remainingCount = await subQueryRepository.CountUnscannedSubsAsync();

            stopwatch.Stop();

            logger.LogInformation(
                "Scanned {@SubIdsCount} subs in {@ElapsedMillis} millis. About {@RemainingCount} subs still need to be scanned.",
                subIdsToScan.Count(),
                stopwatch.ElapsedMilliseconds,
                remainingCount);

            return ScanSubBatchCommandResult.Success;
        }

        private async Task ProcessSubIdsAsync(IEnumerable<long> subIds)
        {
            var downloadTasks = new List<Task<Sub>>(degreeOfParallelism);

            foreach (var subId in subIds)
            {
                downloadTasks.Add(Task.Run(async () => await GetSubDataAsync(subId)));
            }

            var subData = await Task.WhenAll(downloadTasks);

            await subWriteRepository.UpdateSubsAsync(subData);
        }

        private async Task<Sub> GetSubDataAsync(long subId)
        {
            try
            {
                var page = await steamPageFactory.CreateSubPageAsync(subId);
                var friendlyName = page.FriendlyName;
                var isActive = true;

                if (friendlyName == SubPage.UnknownSubName || friendlyName == SteamPage.UnknownPageTitle)
                {
                    logger.LogWarning(
                        "Could not extract friendly name for sub {@SubId} located at address {@Uri}. Marking this sub as inactive.",
                        subId,
                        page.NormalizedAddress.AbsoluteUri);

                    isActive = false;
                }

                return new Sub(page.SubId, page.FriendlyName, isActive);
            }
            catch (SteamPageRemovedException e)
            {
                logger.LogWarning(
                    e,
                    "The Steam page located at address {@Uri} for sub {@SubId} is not accessible. A status code of {@StatusCode} was received while downloading the page contents. Marking this sub as inactive.",
                    e.Uri.AbsoluteUri,
                    subId,
                    e.StatusCode);

                // Return an "unknown" record, because if the database record is not marked as processed, then it'd be kept being retried (after the Redis reservation expires).
                return new Sub(subId, SubPage.UnknownSubName, false);
            }
        }
    }
}