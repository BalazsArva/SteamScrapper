using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamScrapper.Domain.Models.Aggregates;
using SteamScrapper.Domain.Repositories;

namespace SteamScrapper.Infrastructure.RavenDb.Repositories
{
    public class BundleAggregateRepository : IBundleAggregateRepository
    {
        private readonly IDocumentStoreWrapper documentStoreWrapper;

        public BundleAggregateRepository(IDocumentStoreWrapper documentStoreWrapper)
        {
            this.documentStoreWrapper = documentStoreWrapper ?? throw new ArgumentNullException(nameof(documentStoreWrapper));
        }

        public async Task StoreBundleAggregatesAsync(IEnumerable<Bundle> bundles)
        {
            if (bundles is null)
            {
                throw new ArgumentNullException(nameof(bundles));
            }

            if (!bundles.Any())
            {
                return;
            }

            using var session = documentStoreWrapper.DocumentStore.OpenAsyncSession();

            foreach (var bundle in bundles)
            {
                await session.StoreAsync(bundle, bundle.Id);
            }

            await session.SaveChangesAsync();
        }
    }
}