using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class AppRepository : IAppWriteRepository, IAppQueryRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;
        private readonly IDateTimeProvider dateTimeProvider;

        public AppRepository(IDbContextFactory<SteamContext> dbContextFactory, IDateTimeProvider dateTimeProvider)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<int> RegisterUnknownAppsAsync(IEnumerable<int> appIds)
        {
            if (appIds is null)
            {
                throw new ArgumentNullException(nameof(appIds));
            }

            using var context = dbContextFactory.CreateDbContext();

            return await context.RegisterUnknownAppsAsync(appIds);
        }

        public async Task<int> CountUnscannedAppsAsync()
        {
            var now = dateTimeProvider.UtcNow;

            using var context = dbContextFactory.CreateDbContext();

            return await context.Apps.CountAsync(x => x.UtcDateTimeLastModified < now);
        }
    }
}