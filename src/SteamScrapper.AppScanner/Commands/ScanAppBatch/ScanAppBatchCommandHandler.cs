﻿using System;
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
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.AppScanner.Commands.ScanAppBatch
{
    public class ScanAppBatchCommandHandler : IScanAppBatchCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IAppScanningService appScanningService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        private readonly int degreeOfParallelism;

        public ScanAppBatchCommandHandler(
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

            stopwatch.Stop();

            logger.LogInformation(
                "Scanned {@AppIdsCount} apps in {@ElapsedMillis} millis.",
                appIdsToScan.Count(),
                stopwatch.ElapsedMilliseconds);

            return ScanAppBatchCommandResult.Success;
        }

        private async Task ProcessAppIdsAsync(IEnumerable<int> appIds)
        {
            var fetchAppTasks = new List<Task<AppPage>>(degreeOfParallelism);

            foreach (var appId in appIds)
            {
                fetchAppTasks.Add(Task.Run(async () => await steamPageFactory.CreateAppPageAsync(appId)));
            }

            var appPages = await Task.WhenAll(fetchAppTasks);
            var appData = new List<AppData>(appPages.Length);

            for (var i = 0; i < appPages.Length; ++i)
            {
                var appPage = appPages[i];
                var appId = appPage.AppId;
                var friendlyName = appPage.FriendlyName;
                var bannerUrl = appPage.BannerUrl?.AbsoluteUri;

                if (friendlyName == AppPage.UnknownAppName || friendlyName == SteamPage.UnknownPageTitle)
                {
                    logger.LogWarning(
                        "Could not extract friendly name for app {@AppId} located at address {@Uri}.",
                        appId,
                        appPage.NormalizedAddress.AbsoluteUri);
                }

                if (string.IsNullOrWhiteSpace(bannerUrl))
                {
                    logger.LogWarning(
                        "Could not extract banner URL for app {@AppId} located at address {@Uri}.",
                        appId,
                        bannerUrl);
                }

                appData.Add(new AppData(appId, friendlyName, bannerUrl));
            }

            await appScanningService.UpdateAppsAsync(appData);
        }
    }
}