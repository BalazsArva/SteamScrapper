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

namespace SteamScrapper.SubExplorer.BackgroundServices
{
    public class SubExplorerBackgroundService : BackgroundService
    {
        // TODO: Make this configurable.
        private const int DegreeOfParallelism = 8;

        private readonly ISubExplorationService subExplorationService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger<SubExplorerBackgroundService> logger;

        public SubExplorerBackgroundService(
            ISubExplorationService subExplorationService,
            ISteamPageFactory steamPageFactory,
            ILogger<SubExplorerBackgroundService> logger)
        {
            this.subExplorationService = subExplorationService ?? throw new ArgumentNullException(nameof(subExplorationService));
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

                var subIdsToExplore = await subExplorationService.GetNextSubIdsForExplorationAsync(utcNow);
                if (!subIdsToExplore.Any())
                {
                    // TODO: Restart the next day.
                    logger.LogInformation("Could not find more subs to explore.");
                    break;
                }

                var subIdsSegments = subIdsToExplore.Segmentate(DegreeOfParallelism);
                foreach (var subIdsSegment in subIdsSegments)
                {
                    await ProcessSubIdsAsync(subIdsSegment);
                }

                stopwatch.Stop();

                logger.LogInformation(
                    "Processed {@SubIdsCount} subs in {@ElapsedMillis} millis.",
                    subIdsToExplore.Count(),
                    stopwatch.ElapsedMilliseconds);
            }

            logger.LogInformation("Finished exploring subs.");
        }

        private async Task ProcessSubIdsAsync(IEnumerable<int> subIds)
        {
            var fetchSubTasks = new List<Task<SubPage>>(DegreeOfParallelism);

            foreach (var subId in subIds)
            {
                fetchSubTasks.Add(Task.Run(async () => await steamPageFactory.CreateSubPageAsync(subId)));
            }

            var subPages = await Task.WhenAll(fetchSubTasks);
            var subData = new List<SubData>(subPages.Length);

            for (var i = 0; i < subPages.Length; ++i)
            {
                var subPage = subPages[i];
                var subId = subPage.SubId;
                var friendlyName = subPage.FriendlyName;

                if (friendlyName == SubPage.UnknownSubName || friendlyName == SteamPage.UnknownPageTitle)
                {
                    logger.LogWarning(
                        "Could not extract friendly name for sub {@SubId} located at address {@Uri}.",
                        subId,
                        subPage.NormalizedAddress.AbsoluteUri);
                }

                subData.Add(new SubData(subId, friendlyName));
            }

            await subExplorationService.UpdateSubsAsync(subData);
        }
    }
}