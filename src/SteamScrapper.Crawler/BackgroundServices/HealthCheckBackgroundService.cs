using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamScrapper.Common.HealthCheck;

namespace SteamScrapper.Crawler.BackgroundServices
{
    public class HealthCheckBackgroundService : BackgroundService
    {
        private readonly TimeSpan healthCheckPeriod = TimeSpan.FromMinutes(1);
        private readonly TimeSpan healthCheckTimeout = TimeSpan.FromSeconds(15);

        private readonly IEnumerable<IHealthCheckable> healthCheckSources;
        private readonly ILogger logger;

        public HealthCheckBackgroundService(
            IEnumerable<IHealthCheckable> healthCheckSources,
            ILogger<HealthCheckBackgroundService> logger)
        {
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
            var healthCheckTimeoutTask = Task.Delay(this.healthCheckTimeout, combinedCancellationToken);

            await Task.WhenAny(allHealthChecksTask, healthCheckTimeoutTask);

            if (healthCheckTimeoutTask.IsCompleted)
            {
                // Timed out, at least one source failed to respond in time
                logger.LogCritical("Could not get health check responses from all sources in the allowed response time of {@Timeout}.", healthCheckTimeout);

                isHealthy = false;
            }
            else
            {
                // All sources responded, but some of them may have reported unhealthy status.
                // Calling the synchronous .Result is ok here because the Task.WhenAll is completed first if we hit this branch.
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
                logger.LogInformation("Overall health status is healthy.");
            }
            else
            {
                logger.LogCritical("Overall health status is unhealthy.");
            }

            cts.Cancel();
        }

        private static async Task<HealthCheckResult> SafeGetHealthCheckResultAsync(IHealthCheckable source, CancellationToken cancellationToken)
        {
            try
            {
                var t = await source.GetHealthAsync(cancellationToken);

                return t;
            }
            catch (Exception e)
            {
                return new(false, source.GetType().FullName, "An unhandled exception occurred while attempting to retrieve health check response from source.", e);
            }
        }
    }
}