using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class BundleRepository : IBundleRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;

        public BundleRepository(IDbContextFactory<SteamContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<int> RegisterUnknownBundlesAsync(IEnumerable<int> bundleIds)
        {
            if (bundleIds is null)
            {
                throw new ArgumentNullException(nameof(bundleIds));
            }

            using var context = dbContextFactory.CreateDbContext();

            return await context.RegisterUnknownBundlesAsync(bundleIds);
        }
    }
}