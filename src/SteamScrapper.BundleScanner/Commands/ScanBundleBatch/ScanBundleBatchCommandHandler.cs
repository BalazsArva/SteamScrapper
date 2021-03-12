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
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.BundleScanner.Commands.ScanBundleBatch
{
    public class ScanBundleBatchCommandHandler : IScanBundleBatchCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IBundleScanningService bundleScanningService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        private readonly int degreeOfParallelism = 8;

        public ScanBundleBatchCommandHandler(
            IDateTimeProvider dateTimeProvider,
            IBundleScanningService bundleScanningService,
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

            stopwatch.Stop();

            logger.LogInformation(
                "Scanned {@BundleIdsCount} bundles in {@ElapsedMillis} millis.",
                bundleIdsToScan.Count(),
                stopwatch.ElapsedMilliseconds);

            return ScanBundleBatchCommandResult.Success;
        }

        private async Task ProcessBundleIdsAsync(IEnumerable<int> bundleIds)
        {
            var fetchBundleTasks = new List<Task<BundlePage>>(degreeOfParallelism);

            foreach (var bundleId in bundleIds)
            {
                fetchBundleTasks.Add(Task.Run(async () => await steamPageFactory.CreateBundlePageAsync(bundleId)));
            }

            var bundlePages = await Task.WhenAll(fetchBundleTasks);
            var bundleData = new List<BundleData>(bundlePages.Length);

            for (var i = 0; i < bundlePages.Length; ++i)
            {
                var bundlePage = bundlePages[i];
                var bundleId = bundlePage.BundleId;
                var friendlyName = bundlePage.FriendlyName;
                var bannerUrl = bundlePage.BannerUrl?.AbsoluteUri;

                if (friendlyName == BundlePage.UnknownBundleName || friendlyName == SteamPage.UnknownPageTitle)
                {
                    logger.LogWarning(
                        "Could not extract friendly name for bundle {@BundleId} located at address {@Uri}.",
                        bundleId,
                        bundlePage.NormalizedAddress.AbsoluteUri);
                }

                if (string.IsNullOrWhiteSpace(bannerUrl))
                {
                    logger.LogWarning(
                        "Could not extract banner URL for bundle {@BundleId} located at address {@Uri}.",
                        bundleId,
                        bundlePage.NormalizedAddress.AbsoluteUri);
                }

                bundleData.Add(new BundleData(bundleId, friendlyName, bannerUrl));
            }

            await bundleScanningService.UpdateBundlesAsync(bundleData);
        }
    }
}