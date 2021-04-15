using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SteamScrapper.Infrastructure.Database.Context
{
    public class SteamContextHealthChecker : IHealthCheck
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;

        public SteamContextHealthChecker(IDbContextFactory<SteamContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var dbContext = dbContextFactory.CreateDbContext();

                _ = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

                return HealthCheckResult.Healthy("Successfully checked SQL Server health.");
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy("Failed to get query response from SQL Server.", e);
            }
        }
    }
}