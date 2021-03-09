using System;
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
using SteamScrapper.SubScanner.Options;

namespace SteamScrapper.SubScanner.Commands.ProcessSubBatch
{
    public class ProcessSubBatchCommandHandler : IProcessSubBatchCommandHandler
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ISubExplorationService subExplorationService;
        private readonly ISteamPageFactory steamPageFactory;
        private readonly ILogger logger;

        private readonly int degreeOfParallelism = 8;

        public ProcessSubBatchCommandHandler(
            IDateTimeProvider dateTimeProvider,
            ISubExplorationService subExplorationService,
            IOptions<ProcessSubBatchOptions> options,
            ISteamPageFactory steamPageFactory,
            ILogger<ProcessSubBatchCommandHandler> logger)
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
            this.subExplorationService = subExplorationService ?? throw new ArgumentNullException(nameof(subExplorationService));
            this.steamPageFactory = steamPageFactory ?? throw new ArgumentNullException(nameof(steamPageFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            degreeOfParallelism = options.Value.DegreeOfParallelism;

            logger.LogInformation("Using degree of parallelism: {@DegreeOfParallelism}", degreeOfParallelism);
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

            var subIdsSegments = subIdsToExplore.Segmentate(degreeOfParallelism);
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
            var fetchSubTasks = new List<Task<SubPage>>(degreeOfParallelism);

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