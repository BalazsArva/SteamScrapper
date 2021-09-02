using System;
using System.IO;
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
        private readonly IHostEnvironment hostEnvironment;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly HealthCheckService healthCheckService;
        private readonly ILogger logger;

        public HealthCheckBackgroundService(
            IHostEnvironment hostEnvironment,
            IOptions<HealthCheckOptions> healthCheckOptions,
            IHostApplicationLifetime hostApplicationLifetime,
            HealthCheckService healthCheckService,
            ILogger<HealthCheckBackgroundService> logger)
        {
            this.healthCheckOptions = healthCheckOptions?.Value ?? throw new ArgumentNullException(nameof(healthCheckOptions));
            this.hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
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

                var healthReport = await healthCheckService.CheckHealthAsync(stoppingToken);
                if (healthReport.Status == HealthStatus.Healthy)
                {
                    unhealthyCount = 0;

                    logger.LogInformation("Overall health status is {HealthStatus}.", HealthStatus.Healthy);
                }
                else if (healthReport.Status == HealthStatus.Degraded)
                {
                    unhealthyCount = 0;

                    logger.LogWarning("Overall health status is {HealthStatus}.", HealthStatus.Degraded);
                }
                else
                {
                    ++unhealthyCount;

                    logger.LogWarning("Overall health status is {HealthStatus}. Unhealthy status count: {@UnhealthyCount}", HealthStatus.Unhealthy, unhealthyCount);

                    if (unhealthyCount >= healthCheckOptions.MaxUnhealthyCount)
                    {
                        logger.LogCritical("Unhealthy threshold exceeded, terminating application.");

                        // TODO: Later this could be implemented using Docker healthchecks.
                        hostApplicationLifetime.StopApplication();
                    }
                }

                await CreateReportFileIfNeeded(healthReport.Status);
            }
        }

        private async Task CreateReportFileIfNeeded(HealthStatus healthStatus)
        {
            // Retry - perhaps the Docker health check script is reading the file at the same time, and the file is locked.
            // If we fail to write a report file for the 5th time, just ignore, and the script has to interpret that as a
            // sign on unhealthyness. This assumes that the Docker health check script deletes the file once it has read it.
            for (var i = 0; i < 5; ++i)
            {
                try
                {
                    var filePath = Path.Combine(hostEnvironment.ContentRootPath, "health.txt");

                    await File.WriteAllTextAsync(filePath, healthStatus.ToString().ToUpper());
                    return;
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }
        }
    }
}