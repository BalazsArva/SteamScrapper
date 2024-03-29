﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.Providers;
using SteamScrapper.Crawler.Commands.CancelReservations;
using SteamScrapper.Crawler.Commands.ExplorePage;
using SteamScrapper.Crawler.Commands.FinalizeExploration;
using SteamScrapper.Crawler.Commands.RegisterStartingAddresses;
using SteamScrapper.Domain.Services.Exceptions;

namespace SteamScrapper.Crawler.BackgroundServices
{
    public class CrawlerBackgroundService : BackgroundService
    {
        private const int DelaySecondsOnUnknownError = 60;
        private const int DelaySecondsOnRateLimitExceededError = 300;

        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IFinalizeExplorationCommandHandler finalizeExplorationCommandHandler;
        private readonly IExplorePageCommandHandler explorePageCommandHandler;
        private readonly ICancelReservationsCommandHandler cancelReservationsCommandHandler;
        private readonly IRegisterStartingAddressesCommandHandler registerStartingAddressesCommandHandler;
        private readonly ILogger logger;

        public CrawlerBackgroundService(
            IDateTimeProvider dateTimeProvider,
            IFinalizeExplorationCommandHandler finalizeExplorationCommandHandler,
            IExplorePageCommandHandler explorePageCommandHandler,
            ICancelReservationsCommandHandler cancelReservationsCommandHandler,
            ILogger<CrawlerBackgroundService> logger,
            IRegisterStartingAddressesCommandHandler registerStartingAddressesCommandHandler)
        {
            this.dateTimeProvider = dateTimeProvider;
            this.finalizeExplorationCommandHandler = finalizeExplorationCommandHandler ?? throw new ArgumentNullException(nameof(finalizeExplorationCommandHandler));
            this.explorePageCommandHandler = explorePageCommandHandler ?? throw new ArgumentNullException(nameof(explorePageCommandHandler));
            this.cancelReservationsCommandHandler = cancelReservationsCommandHandler ?? throw new ArgumentNullException(nameof(cancelReservationsCommandHandler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.registerStartingAddressesCommandHandler = registerStartingAddressesCommandHandler ?? throw new ArgumentNullException(nameof(registerStartingAddressesCommandHandler));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await registerStartingAddressesCommandHandler.RegisterStartingAddressesAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delaySeconds = 0;

                    try
                    {
                        var result = await explorePageCommandHandler.ExplorePageAsync(stoppingToken);
                        if (result == ExplorePageCommandResult.NoMoreItems)
                        {
                            logger.LogInformation("The crawler process has finished processing all links.");
                            break;
                        }
                    }
                    catch (SteamRateLimitExceededException e)
                    {
                        logger.LogError(e, "The request rate limit has been exceeded during the crawling process. Retrying in {@Delay} seconds.", DelaySecondsOnRateLimitExceededError);
                        delaySeconds = DelaySecondsOnRateLimitExceededError;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "An unhandled error occurred during the crawling process. Retrying in {@Delay} seconds.", DelaySecondsOnUnknownError);
                        delaySeconds = DelaySecondsOnUnknownError;
                    }

                    if (delaySeconds > 0)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                }

                await finalizeExplorationCommandHandler.FinalizeExplorationAsync();

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                // Add 5 minutes to ensure that minor time imprecisions won't cause the process to continue on the same day.
                var tomorrowMorning = dateTimeProvider.UtcNow.Date.AddDays(1).AddMinutes(5);
                var waitTime = tomorrowMorning - dateTimeProvider.UtcNow;

                try
                {
                    await Task.Delay(waitTime, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            await cancelReservationsCommandHandler.CancelReservations();

            logger.LogInformation("Appplication shutdown requested, the crawler service has been terminated.");
        }
    }
}