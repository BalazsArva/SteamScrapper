using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamScrapper.AppScanner.Options;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;
using SteamScrapper.Domain.Services.Exceptions;

namespace SteamScrapper.AppScanner.Commands.ScanAppBatch
{
    public class ScanAppBatchCommandHandler : IScanAppBatchCommandHandler
    {
        private readonly IAppQueryRepository appQueryRepository;
        private readonly IAppWriteRepository appWriteRepository;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IAppScanningService appScanningService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        private readonly int degreeOfParallelism;

        public ScanAppBatchCommandHandler(
            IAppQueryRepository appQueryRepository,
            IAppWriteRepository appWriteRepository,
            IDateTimeProvider dateTimeProvider,
            IAppScanningService appScanningService,
            ISteamPageFactory steamPageFactory,
            IOptions<ScanAppBatchOptions> options,
            ILogger<ScanAppBatchCommandHandler> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Value is null)
            {
                throw new ArgumentException(
                    "The provided configuration object does not contain valid settings for app batch processing.",
                    nameof(options));
            }

            this.appQueryRepository = appQueryRepository ?? throw new ArgumentNullException(nameof(appQueryRepository));
            this.appWriteRepository = appWriteRepository ?? throw new ArgumentNullException(nameof(appWriteRepository));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.appScanningService = appScanningService ?? throw new ArgumentNullException(nameof(appScanningService));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            degreeOfParallelism = options.Value.DegreeOfParallelism;

            logger.LogInformation("Using degree of parallelism: {@DegreeOfParallelism}", degreeOfParallelism);
        }

        public async Task<ScanAppBatchCommandResult> ScanAppBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var utcNow = dateTimeProvider.UtcNow;

            var appIdsToScan = await appScanningService.GetNextAppIdsForScanningAsync(utcNow);
            if (!appIdsToScan.Any())
            {
                logger.LogInformation("Could not find more apps to scan.");
                return ScanAppBatchCommandResult.NoMoreItems;
            }

            var appIdsSegments = appIdsToScan.Segmentate(degreeOfParallelism);
            foreach (var appIdsSegment in appIdsSegments)
            {
                await ProcessAppIdsAsync(appIdsSegment);
            }

            var remainingCount = await appQueryRepository.CountUnscannedAppsAsync();

            stopwatch.Stop();

            logger.LogInformation(
                "Scanned {@AppIdsCount} apps in {@ElapsedMillis} millis. About {@RemainingCount} apps still need to be scanned.",
                appIdsToScan.Count(),
                stopwatch.ElapsedMilliseconds,
                remainingCount);

            return ScanAppBatchCommandResult.Success;
        }

        private async Task ProcessAppIdsAsync(IEnumerable<long> appIds)
        {
            var downloadTasks = new List<Task<AppData>>(degreeOfParallelism);

            foreach (var appId in appIds)
            {
                downloadTasks.Add(Task.Run(async () => await GetAppDataAsync(appId)));
            }

            var appData = await Task.WhenAll(downloadTasks);

            await appWriteRepository.UpdateAppsAsync(appData);
        }

        private async Task<AppData> GetAppDataAsync(long appId)
        {
            try
            {
                var page = await steamPageFactory.CreateAppPageAsync(appId);
                var friendlyName = page.FriendlyName;
                var bannerUrl = page.BannerUrl?.AbsoluteUri;
                var isActive = true;

                if (friendlyName == AppPage.UnknownAppName || friendlyName == SteamPage.UnknownPageTitle)
                {
                    logger.LogWarning(
                        "Could not extract friendly name for app {@AppId} located at address {@Uri}.",
                        appId,
                        page.NormalizedAddress.AbsoluteUri);

                    isActive = false;
                }

                if (string.IsNullOrWhiteSpace(bannerUrl))
                {
                    logger.LogWarning(
                        "Could not extract banner URL for app {@AppId} located at address {@Uri}.",
                        appId,
                        bannerUrl);
                }

                return new AppData(appId, friendlyName, bannerUrl, isActive);
            }
            catch (SteamPageRemovedException e)
            {
                logger.LogWarning(
                    e,
                    "The Steam page located at address {@Uri} for app {@AppId} is not accessible. A status code of {@StatusCode} was received while downloading the page contents.",
                    e.Uri.AbsoluteUri,
                    appId,
                    e.StatusCode);

                // Return an "unknown" record, because if the database record is not marked as processed, then it'd be kept being retried (after the Redis reservation expires).
                return new AppData(appId, AppPage.UnknownAppName, null, false);
            }
        }
    }
}