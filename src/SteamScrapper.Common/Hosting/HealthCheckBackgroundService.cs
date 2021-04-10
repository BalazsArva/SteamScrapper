using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamScrapper.Common.HealthCheck;
using SteamScrapper.Common.Options;

namespace SteamScrapper.Common.Hosting
{
    public class HealthCheckBackgroundService : BackgroundService
    {
        private readonly HealthCheckOptions healthCheckOptions;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly IEnumerable<IHealthCheckable> healthCheckSources;
        private readonly ILogger logger;

        public HealthCheckBackgroundService(
            IOptions<HealthCheckOptions> healthCheckOptions,
            IHostApplicationLifetime hostApplicationLifetime,
            IEnumerable<IHealthCheckable> healthCheckSources,
            ILogger<HealthCheckBackgroundService> logger)
        {
            this.healthCheckOptions = healthCheckOptions?.Value ?? throw new ArgumentNullException(nameof(healthCheckOptions));
            this.hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
            this.healthCheckSources = healthCheckSources ?? throw new ArgumentNullException(nameof(healthCheckSources));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!healthCheckSources.Any())
            {
                logger.LogWarning("No health check sources found. Periodic health check service will not be started.");
                return;
            }

            var unhealthyCount = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(healthCheckOptions.HealthCheckPeriod, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                var isHealthyOverall = await CheckHealthAsync(healthCheckSources, stoppingToken);
                if (isHealthyOverall)
                {
                    unhealthyCount = 0;

                    logger.LogInformation("Overall health status is healthy.");
                }
                else
                {
                    ++unhealthyCount;

                    logger.LogCritical("Overall health status is unhealthy. Unhealthy status count: {@UnhealthyCount}", unhealthyCount);

                    if (unhealthyCount >= healthCheckOptions.MaxUnhealthyCount)
                    {
                        logger.LogCritical("Unhealthy threshold exceeded, terminating application.");

                        // TODO: Later this could be implemented using Docker healthchecks.
                        hostApplicationLifetime.StopApplication();
                    }
                }
            }
        }

        private async Task<bool> CheckHealthAsync(IEnumerable<IHealthCheckable> sources, CancellationToken cancellationToken)
        {
            logger.LogInformation("Checking health status...");

            var cts = new CancellationTokenSource();
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token;

            var isHealthy = true;
            var healthCheckTasks = new List<Task<bool>>();
            var healthCheckTimeout = healthCheckOptions.HealthCheckTimeout;

            foreach (var source in sources)
            {
                healthCheckTasks.Add(SafeCheckIsHealthyAsync(source, combinedCancellationToken));
            }

            var allHealthChecksTask = Task.WhenAll(healthCheckTasks);
            var healthCheckTimeoutTask = Task.Delay(healthCheckTimeout, combinedCancellationToken);

            await Task.WhenAny(allHealthChecksTask, healthCheckTimeoutTask);

            if (healthCheckTimeoutTask.IsCompletedSuccessfully)
            {
                // Timed out, at least one source failed to respond in time.
                logger.LogCritical("Could not get health check responses from all sources in the allowed response time of {@Timeout}.", healthCheckTimeout);

                isHealthy = false;
            }

            if (allHealthChecksTask.IsCompletedSuccessfully)
            {
                // All sources responded, but some of them may have reported unhealthy status.
                // Calling the synchronous .Result is ok here because the Task.WhenAll is completed successfully if we hit this branch.
                isHealthy = allHealthChecksTask.Result.All(x => x);
            }

            cts.Cancel();

            return isHealthy;
        }

        private async Task<bool> SafeCheckIsHealthyAsync(IHealthCheckable source, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckResult = await source.GetHealthAsync(cancellationToken);

                if (healthCheckResult.IsHealthy)
                {
                    // {@HealthStatus} is a parameter to make logs searchable based on this property.
                    logger.LogInformation(
                        "Health check source {@ReporterName} reported health status as {@HealthStatus}.",
                        healthCheckResult.ReporterName,
                        "healthy");
                }
                else
                {
                    logger.LogCritical(
                        healthCheckResult.Exception,
                        "Health check source {@ReporterName} reported health status as {@HealthStatus}. Reason given: {@Reason}",
                        healthCheckResult.ReporterName,
                        "unhealthy",
                        healthCheckResult.Reason);
                }

                return healthCheckResult.IsHealthy;
            }
            catch (Exception e)
            {
                var sourceName = source.GetType().FullName;

                logger.LogCritical(
                    e,
                    "An unhandled exception occurred while attempting to retrieve health check response from source {@ReporterName}.",
                    sourceName);

                return false;
            }
        }
    }
}
