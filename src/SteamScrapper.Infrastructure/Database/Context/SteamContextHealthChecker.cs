using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Common.HealthCheck;

namespace SteamScrapper.Infrastructure.Database.Context
{
    public class SteamContextHealthChecker : IHealthCheckable
    {
        private static readonly string ReporterName = typeof(SteamContextHealthChecker).FullName;

        private readonly IDbContextFactory<SteamContext> dbContextFactory;

        public SteamContextHealthChecker(IDbContextFactory<SteamContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<HealthCheckResult> GetHealthAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var context = dbContextFactory.CreateDbContext();

                _ = await context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

                return new(true, ReporterName);
            }
            catch (Exception e)
            {
                return new(false, ReporterName, "Failed to get query response from SQL Server.", e);
            }
        }
    }
}