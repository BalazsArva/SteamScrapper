using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamScrapper.BundleScanner.Options;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;
using SteamScrapper.Domain.Services.Exceptions;

namespace SteamScrapper.BundleScanner.Commands.ScanBundleBatch
{
    public class ScanBundleBatchCommandHandler : IScanBundleBatchCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IBundleScanningService bundleScanningService;
        private readonly IBundleQueryRepository bundleQueryRepository;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        private readonly int degreeOfParallelism = 8;

        public ScanBundleBatchCommandHandler(
            IDateTimeProvider dateTimeProvider,
            IBundleScanningService bundleScanningService,
            IBundleQueryRepository bundleQueryRepository,
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
            this.bundleQueryRepository = bundleQueryRepository ?? throw new ArgumentNullException(nameof(bundleQueryRepository));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            degreeOfParallelism = options.Value.DegreeOfParallelism;

            logger.LogInformation("Using degree of parallelism: {@DegreeOfParallelism}", degreeOfParallelism);
        }

        public async Task<ScanBundleBatchCommandResult> ScanBundleBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var utcNow = dateTimeProvider.UtcNow;

            var bundleIdsToScan = await bundleScanningService.GetNextBundleIdsForScanningAsync(utcNow);
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

            var remainingCount = await bundleQueryRepository.CountUnscannedBundlesAsync();

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
            var downloadTasks = new List<Task<BundleData>>(degreeOfParallelism);

            foreach (var bundleId in bundleIds)
            {
                downloadTasks.Add(Task.Run(async () => await GetBundleDataAsync(bundleId)));
            }

            var bundleData = await Task.WhenAll(downloadTasks);

            await bundleScanningService.UpdateBundlesAsync(bundleData);
        }

        private async Task<BundleData> GetBundleDataAsync(long bundleId)
        {
            try
            {
                var page = await steamPageFactory.CreateBundlePageAsync(bundleId);
                var friendlyName = page.FriendlyName;
                var bannerUrl = page.BannerUrl?.AbsoluteUri;

                if (friendlyName == BundlePage.UnknownBundleName || friendlyName == SteamPage.UnknownPageTitle)
                {
                    logger.LogWarning(
                        "Could not extract friendly name for bundle {@BundleId} located at address {@Uri}.",
                        bundleId,
                        page.NormalizedAddress.AbsoluteUri);
                }

                if (string.IsNullOrWhiteSpace(bannerUrl))
                {
                    logger.LogWarning(
                        "Could not extract banner URL for bundle {@BundleId} located at address {@Uri}.",
                        bundleId,
                        page.NormalizedAddress.AbsoluteUri);
                }

                return new BundleData(bundleId, friendlyName, bannerUrl);
            }
            catch (SteamPageRemovedException e)
            {
                logger.LogWarning(
                    e,
                    "The Steam page located at address {@Uri} for bundle {@BundleId} is not accessible. A status code of {@StatusCode} was received while downloading the page contents.",
                    e.Uri.AbsoluteUri,
                    bundleId,
                    e.StatusCode);

                // Return an "unknown" record, because if the database record is not marked as processed, then it'd be kept being retried (after the Redis reservation expires).
                return new BundleData(bundleId, BundlePage.UnknownBundleName, null);
            }
        }
    }
}