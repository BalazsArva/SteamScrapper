﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.AppExplorer.Commands.ProcessAppBatch
{
    public class ProcessAppBatchCommandHandler : IProcessAppBatchCommandHandler
    {
        // TODO: Make this configurable.
        private const int DegreeOfParallelism = 8;

        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IAppExplorationService appExplorationService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        public ProcessAppBatchCommandHandler(
            IDateTimeProvider dateTimeProvider,
            IAppExplorationService appExplorationService,
            ISteamPageFactory steamPageFactory,
            ILogger<ProcessAppBatchCommandHandler> logger)
        {
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.appExplorationService = appExplorationService ?? throw new ArgumentNullException(nameof(appExplorationService));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProcessAppBatchCommandResult> ProcessAppBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var utcNow = dateTimeProvider.UtcNow;

            var appIdsToExplore = await appExplorationService.GetNextAppIdsForExplorationAsync(utcNow);
            if (!appIdsToExplore.Any())
            {
                logger.LogInformation("Could not find more apps to explore.");
                return ProcessAppBatchCommandResult.NoMoreItems;
            }

            var appIdsSegments = appIdsToExplore.Segmentate(DegreeOfParallelism);
            foreach (var appIdsSegment in appIdsSegments)
            {
                await ProcessAppIdsAsync(appIdsSegment);
            }

            stopwatch.Stop();

            logger.LogInformation(
                "Processed {@AppIdsCount} apps in {@ElapsedMillis} millis.",
                appIdsToExplore.Count(),
                stopwatch.ElapsedMilliseconds);

            return ProcessAppBatchCommandResult.Success;
        }

        private async Task ProcessAppIdsAsync(IEnumerable<int> appIds)
        {
            var fetchAppTasks = new List<Task<AppPage>>(DegreeOfParallelism);

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

            await appExplorationService.UpdateAppsAsync(appData);
        }
    }
}