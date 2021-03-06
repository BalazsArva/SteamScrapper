using System;
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

namespace SteamScrapper.SubExplorer.Commands.ProcessSubBatch
{
    public class ProcessSubBatchCommandHandler : IProcessSubBatchCommandHandler
    {
        // TODO: Make this configurable.
        private const int DegreeOfParallelism = 8;

        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ISubExplorationService subExplorationService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        public ProcessSubBatchCommandHandler(
            IDateTimeProvider dateTimeProvider,
            ISubExplorationService subExplorationService,
            ISteamPageFactory steamPageFactory,
            ILogger<ProcessSubBatchCommandHandler> logger)
        {
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            this.subExplorationService = subExplorationService ?? throw new ArgumentNullException(nameof(subExplorationService));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProcessSubBatchCommandResult> ProcessSubBatchAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var utcNow = dateTimeProvider.UtcNow;

            var subIdsToExplore = await subExplorationService.GetNextSubIdsForExplorationAsync(utcNow);
            if (!subIdsToExplore.Any())
            {
                logger.LogInformation("Could not find more subs to explore.");
                return ProcessSubBatchCommandResult.NoMoreItems;
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

            return ProcessSubBatchCommandResult.Success;
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