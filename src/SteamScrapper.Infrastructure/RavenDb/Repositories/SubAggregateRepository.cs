using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamScrapper.Domain.Models.Aggregates;
using SteamScrapper.Domain.Repositories;

namespace SteamScrapper.Infrastructure.RavenDb.Repositories
{
    public class SubAggregateRepository : ISubAggregateRepository
    {
        private readonly IDocumentStoreWrapper documentStoreWrapper;
        private readonly string collectionPrefix;

        public SubAggregateRepository(IDocumentStoreWrapper documentStoreWrapper)
        {
            this.documentStoreWrapper = documentStoreWrapper ?? throw new ArgumentNullException(nameof(documentStoreWrapper));

            collectionPrefix =
                documentStoreWrapper.DocumentStore.Conventions.GetCollectionName(typeof(Sub)) +
                documentStoreWrapper.DocumentStore.Conventions.IdentityPartsSeparator;
        }

        public async Task StoreSubAggregatesAsync(IEnumerable<Sub> subs)
        {
            if (subs is null)
            {
                throw new ArgumentNullException(nameof(subs));
            }

            if (!subs.Any())
            {
                return;
            }

            using var session = documentStoreWrapper.DocumentStore.OpenAsyncSession();

            foreach (var sub in subs)
            {
                await session.StoreAsync(sub, collectionPrefix + sub.Id);
            }

            await session.SaveChangesAsync();
        }
    }
}