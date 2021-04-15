using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamScrapper.Common.Options;

namespace SteamScrapper.Common.Hosting
{
    public class HealthCheckBackgroundService : BackgroundService
    {
        private readonly HealthCheckOptions healthCheckOptions;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly HealthCheckService healthCheckService;
        private readonly ILogger logger;

        public HealthCheckBackgroundService(
            IOptions<HealthCheckOptions> healthCheckOptions,
            IHostApplicationLifetime hostApplicationLifetime,
            HealthCheckService healthCheckService,
            ILogger<HealthCheckBackgroundService> logger)
        {
            this.healthCheckOptions = healthCheckOptions?.Value ?? throw new ArgumentNullException(nameof(healthCheckOptions));
            this.hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
            this.healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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

                var healthCheckResult = await healthCheckService.CheckHealthAsync(stoppingToken);
                if (healthCheckResult.Status == HealthStatus.Healthy)
                {
                    unhealthyCount = 0;

                    logger.LogInformation("Overall health status is healthy.");
                }
                else if (healthCheckResult.Status == HealthStatus.Degraded)
                {
                    unhealthyCount = 0;

                    logger.LogWarning("Overall health status is degraded.");
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
    }
}