﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamScrapper.BundleScanner.Options;
using SteamScrapper.BundleScanner.Services;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Repositories.Models;
using SteamScrapper.Domain.Services.Exceptions;

namespace SteamScrapper.BundleScanner.Commands.ScanBundleBatch
{
    public class ScanBundleBatchCommandHandler : IScanBundleBatchCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IBundleScanningService bundleScanningService;
        private readonly IBundleWriteRepository bundleWriteRepository;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        private readonly int degreeOfParallelism = 8;

        public ScanBundleBatchCommandHandler(
            IDateTimeProvider dateTimeProvider,
            IBundleScanningService bundleScanningService,
            IBundleWriteRepository bundleWriteRepository,
            IOptions<ScanBundleBatchOptions> options,
            ISteamPageFactory steamPageFactory,
            ILogger<ScanBundleBatchCommandHandler> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Value is null)
            {
                throw new ArgumentException(
                    "The provided configuration object does not contain valid settings for bundle batch processing.",
                    nameof(options));
            }

            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.bundleScanningService = bundleScanningService ?? throw new ArgumentNullException(nameof(bundleScanningService));
            this.bundleWriteRepository = bundleWriteRepository ?? throw new ArgumentNullException(nameof(bundleWriteRepository));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            degreeOfParallelism = options.Value.DegreeOfParallelism;

            logger.LogInformation("Using degree of parallelism: {@DegreeOfParallelism}", degreeOfParallelism);
        }

        public async Task<ScanBundleBatchCommandResult> ScanBundleBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            var bundleIdsToScan = await bundleScanningService.GetNextBundleIdsForScanningAsync();
            if (!bundleIdsToScan.Any())
            {
                logger.LogInformation("Could not find more bundles to scan.");
                return ScanBundleBatchCommandResult.NoMoreItems;
            }

            var bundleIdsSegments = bundleIdsToScan.Segmentate(degreeOfParallelism);
            foreach (var bundleIdsSegment in bundleIdsSegments)
            {
                await ProcessBundleIdsAsync(bundleIdsSegment);
            }

            var remainingCount = await bundleScanningService.CountUnscannedBundlesAsync();

            stopwatch.Stop();

            logger.LogInformation(
                "Scanned {@BundleIdsCount} bundles in {@ElapsedMillis} millis. About {@RemainingCount} bundles still need to be scanned.",
                bundleIdsToScan.Count(),
                stopwatch.ElapsedMilliseconds,
                remainingCount);

            return ScanBundleBatchCommandResult.Success;
        }

        private async Task ProcessBundleIdsAsync(IEnumerable<long> bundleIds)
        {
            var downloadTasks = new List<Task<Bundle>>(degreeOfParallelism);

            foreach (var bundleId in bundleIds)
            {
                downloadTasks.Add(Task.Run(async () => await GetBundleDataAsync(bundleId)));
            }

            var bundleData = await Task.WhenAll(downloadTasks);

            await bundleWriteRepository.UpdateBundlesAsync(bundleData);
        }

        private async Task<Bundle> GetBundleDataAsync(long bundleId)
        {
            try
            {
                var page = await steamPageFactory.CreateBundlePageAsync(bundleId);
                var friendlyName = page.FriendlyName;
                var bundlePrice = page.Price;
                var bannerUrl = page.BannerUrl?.AbsoluteUri;
                var isActive = true;
                Price dbPrice = null;

                if (friendlyName == BundlePage.UnknownBundleName || friendlyName == SteamPage.UnknownPageTitle)
                {
                    logger.LogWarning(
                        "Could not extract friendly name for bundle {@BundleId} located at address {@Uri}. Marking this bundle as inactive.",
                        bundleId,
                        page.NormalizedAddress.AbsoluteUri);

                    isActive = false;
                }

                if (bundlePrice == Domain.Models.Price.Unknown)
                {
                    logger.LogWarning(
                        "Could not extract price for bundle {@BundleId} located at address {@Uri}. Marking this bundle as inactive.",
                        bundleId,
                        page.NormalizedAddress.AbsoluteUri);

                    isActive = false;
                }
                else
                {
                    dbPrice = new Price(bundlePrice.NormalPrice, bundlePrice.DiscountPrice, bundlePrice.Currency, dateTimeProvider.UtcNow);
                }

                if (string.IsNullOrWhiteSpace(bannerUrl))
                {
                    logger.LogWarning(
                        "Could not extract banner URL for bundle {@BundleId} located at address {@Uri}.",
                        bundleId,
                        page.NormalizedAddress.AbsoluteUri);
                }

                return new Bundle(bundleId, friendlyName, bannerUrl, isActive, dbPrice);
            }
            catch (SteamPageRemovedException e)
            {
                logger.LogWarning(
                    e,
                    "The Steam page located at address {@Uri} for bundle {@BundleId} is not accessible. A status code of {@StatusCode} was received while downloading the page contents. Marking this bundle as inactive.",
                    e.Uri.AbsoluteUri,
                    bundleId,
                    e.StatusCode);

                // Return an "unknown" record, because if the database record is not marked as processed, then it'd be kept being retried (after the Redis reservation expires).
                return new Bundle(bundleId, BundlePage.UnknownBundleName, null, false);
            }
        }
    }
}