﻿using System;
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
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;
using SteamScrapper.Domain.Services.Exceptions;
using SteamScrapper.SubScanner.Options;

namespace SteamScrapper.SubScanner.Commands.ScanSubBatch
{
    public class ScanSubBatchCommandHandler : IScanSubBatchCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ISubScanningService subScanningService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        private readonly int degreeOfParallelism = 8;

        public ScanSubBatchCommandHandler(
            IDateTimeProvider dateTimeProvider,
            ISubScanningService subScanningService,
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

            var remainingCount = await subScanningService.GetCountOfUnscannedSubsAsync();

            stopwatch.Stop();

            logger.LogInformation(
                "Scanned {@SubIdsCount} subs in {@ElapsedMillis} millis. About {@RemainingCount} apps still need to be scanned.",
                subIdsToScan.Count(),
                stopwatch.ElapsedMilliseconds,
                remainingCount);

            return ScanSubBatchCommandResult.Success;
        }

        private async Task ProcessSubIdsAsync(IEnumerable<int> subIds)
        {
            var downloadTasks = new List<Task<SubData>>(degreeOfParallelism);

            foreach (var subId in subIds)
            {
                downloadTasks.Add(Task.Run(async () => await GetSubDataAsync(subId)));
            }

            var subData = await Task.WhenAll(downloadTasks);

            await subScanningService.UpdateSubsAsync(subData);
        }

        private async Task<SubData> GetSubDataAsync(int subId)
        {
            try
            {
                var page = await steamPageFactory.CreateSubPageAsync(subId);
                var friendlyName = page.FriendlyName;

                if (friendlyName == SubPage.UnknownSubName || friendlyName == SteamPage.UnknownPageTitle)
                {
                    logger.LogWarning(
                        "Could not extract friendly name for sub {@SubId} located at address {@Uri}.",
                        subId,
                        page.NormalizedAddress.AbsoluteUri);
                }

                return new SubData(page.SubId, page.FriendlyName);
            }
            catch (SteamPageRemovedException e)
            {
                logger.LogWarning(
                    e,
                    "The Steam page located at address {@Uri} for sub {@SubId} is not accessible. A status code of {@StatusCode} was received while downloading the page contents.",
                    e.Uri.AbsoluteUri,
                    subId,
                    e.StatusCode);

                // Return an "unknown" record, because if the database record is not marked as processed, then it'd be kept being retried (after the Redis reservation expires).
                return new SubData(subId, SubPage.UnknownSubName);
            }
        }
    }
}