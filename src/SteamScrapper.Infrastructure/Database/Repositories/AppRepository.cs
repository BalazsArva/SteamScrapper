﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class AppRepository : IAppRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;

        public AppRepository(IDbContextFactory<SteamContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
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
    }
}