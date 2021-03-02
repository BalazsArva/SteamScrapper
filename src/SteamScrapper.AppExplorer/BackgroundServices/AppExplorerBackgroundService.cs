using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Extensions;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.PageModels;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.AppExplorer.BackgroundServices
{
    public class AppExplorerBackgroundService : BackgroundService
    {
        // TODO: Make this configurable.
        private const int DegreeOfParallelism = 8;

        private readonly IAppExplorationService appExplorationService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger<AppExplorerBackgroundService> logger;

        public AppExplorerBackgroundService(
            IAppExplorationService appExplorationService,
            ISteamPageFactory steamPageFactory,
            ILogger<AppExplorerBackgroundService> logger)
        {
            this.appExplorationService = appExplorationService ?? throw new ArgumentNullException(nameof(appExplorationService));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var utcNow = DateTime.UtcNow;

            // TODO: Move the guts to a handler that executes a single iteration. This makes testing easier.
            while (!stoppingToken.IsCancellationRequested)
            {
                var stopwatch = Stopwatch.StartNew();

                utcNow = DateTime.UtcNow;

                var appIdsToExplore = await appExplorationService.GetNextAppIdsForExplorationAsync(utcNow);
                if (!appIdsToExplore.Any())
                {
                    // TODO: Restart the next day.
                    logger.LogInformation("Could not find more apps to explore.");
                    break;
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
            }

            logger.LogInformation("Finished exploring apps.");
        }

        private async Task ProcessAppIdsAsync(IEnumerable<int> appIds)
        {
            var fetchAppTasks = new List<Task<AppPage>>(DegreeOfParallelism);

            foreach (var appId in appIds)
            {
                fetchAppTasks.Add(Task.Run(async () => await steamPageFactory.CreateAppPageAsync(appId)));
            }

            var appPages = await Task.WhenAll(fetchAppTasks);

            var appData = appPages.Select(x => new AppData(x.AppId, x.FriendlyName, null)).ToList();

            await appExplorationService.UpdateAppsAsync(appData);
        }
    }
}