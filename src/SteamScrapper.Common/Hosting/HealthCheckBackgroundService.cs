using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.HealthCheck;

namespace SteamScrapper.Common.Hosting
{
    public class HealthCheckBackgroundService : BackgroundService
    {
        private const int MaxUnhealthyCount = 3;

        private readonly TimeSpan healthCheckPeriod = TimeSpan.FromMinutes(1);
        private readonly TimeSpan healthCheckTimeout = TimeSpan.FromSeconds(15);

        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly IEnumerable<IHealthCheckable> healthCheckSources;
        private readonly ILogger logger;

        private int unhealthyCount = 0;

        public HealthCheckBackgroundService(
            IHostApplicationLifetime hostApplicationLifetime,
            IEnumerable<IHealthCheckable> healthCheckSources,
            ILogger<HealthCheckBackgroundService> logger)
        {
            this.hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
            this.healthCheckSources = healthCheckSources ?? throw new ArgumentNullException(nameof(healthCheckSources));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(healthCheckPeriod, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                await CheckHealthAsync(healthCheckSources, stoppingToken);
            }
        }

        private async Task CheckHealthAsync(IEnumerable<IHealthCheckable> sources, CancellationToken cancellationToken)
        {
            logger.LogInformation("Checking health status...");

            var cts = new CancellationTokenSource();
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token;

            var isHealthy = true;
            var healthCheckTasks = new List<Task<HealthCheckResult>>();

            foreach (var source in sources)
            {
                healthCheckTasks.Add(SafeGetHealthCheckResultAsync(source, combinedCancellationToken));
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
                // The tasks passed to Task.WhenAll(...) are safe wrappers, so it's also safe to assume that there's no exception,
                // every item has an outcome (which wraps any exceptions).
                foreach (var healthCheckResult in allHealthChecksTask.Result)
                {
                    if (healthCheckResult.IsHealthy)
                    {
                        continue;
                    }

                    isHealthy = false;

                    logger.LogCritical(
                        healthCheckResult.Exception,
                        "Health check source {@ReporterName} reported health status as unhealthy. Reason given: {@Reason}",
                        healthCheckResult.ReporterName,
                        healthCheckResult.Reason);
                }
            }

            if (isHealthy)
            {
                unhealthyCount = 0;

                logger.LogInformation("Overall health status is healthy.");
            }
            else
            {
                ++unhealthyCount;

                logger.LogCritical("Overall health status is unhealthy. Unhealthy status count: {@UnhealthyCount}", unhealthyCount);

                if (unhealthyCount >= MaxUnhealthyCount)
                {
                    logger.LogCritical("Unhealthy threshold exceeded, terminating application.");

                    // TODO: Later this could be implemented using Docker healthchecks.
                    hostApplicationLifetime.StopApplication();
                }
            }

            cts.Cancel();
        }

        private static async Task<HealthCheckResult> SafeGetHealthCheckResultAsync(IHealthCheckable source, CancellationToken cancellationToken)
        {
            try
            {
                return await source.GetHealthAsync(cancellationToken);
            }
            catch (Exception e)
            {
                var sourceName = source.GetType().FullName;

                return new(false, sourceName, "An unhandled exception occurred while attempting to retrieve health check response from source.", e);
            }
        }
    }
}