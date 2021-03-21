using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class SubRepository : ISubRepository
    {
        private readonly IDbContextFactory<SteamContext> dbContextFactory;

        public SubRepository(IDbContextFactory<SteamContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<int> RegisterUnknownSubsAsync(IEnumerable<int> subIds)
        {
            if (subIds is null)
            {
                throw new ArgumentNullException(nameof(subIds));
            }

            using var context = dbContextFactory.CreateDbContext();

            return await context.RegisterUnknownSubsAsync(subIds);
        }
    }
}