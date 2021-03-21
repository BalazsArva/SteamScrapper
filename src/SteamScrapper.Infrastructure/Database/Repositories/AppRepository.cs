using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class AppRepository : IAppRepository
    {
        private readonly SteamContext context;

        public AppRepository(SteamContext steamContext)
        {
            context = steamContext ?? throw new ArgumentNullException(nameof(steamContext));
        }

        public async Task<int> RegisterUnknownAppsAsync(IEnumerable<int> appIds)
        {
            if (appIds is null)
            {
                throw new ArgumentNullException(nameof(appIds));
            }

            return await context.RegisterUnknownAppsAsync(appIds);
        }
    }
}