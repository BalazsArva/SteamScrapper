using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raven.Client.Documents.Operations;

namespace SteamScrapper.Infrastructure.RavenDb
{
    public class RavenDbHealthCheck : IHealthCheck
    {
        private static readonly GetStatisticsOperation GetStatisticsOperation = new GetStatisticsOperation();

        private readonly IDocumentStoreWrapper documentStoreWrapper;

        public RavenDbHealthCheck(IDocumentStoreWrapper documentStoreWrapper)
        {
            this.documentStoreWrapper = documentStoreWrapper ?? throw new ArgumentNullException(nameof(documentStoreWrapper));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _ = await documentStoreWrapper.DocumentStore.Maintenance.SendAsync(GetStatisticsOperation, cancellationToken);

                return HealthCheckResult.Healthy("Successfully checked RavenDB health.");
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy("Failed to get response from RavenDB.", e);
            }
        }
    }
}