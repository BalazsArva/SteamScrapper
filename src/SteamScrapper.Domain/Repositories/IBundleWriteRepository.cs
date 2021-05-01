using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Repositories.Models;

namespace SteamScrapper.Domain.Repositories
{
    public interface IBundleWriteRepository
    {
        Task AddBundleAggregationsAsync(IEnumerable<long> bundleIds, DateTime performedAt);

        Task<int> RegisterUnknownBundlesAsync(IEnumerable<long> bundleIds);

        Task UpdateBundlesAsync(IEnumerable<Bundle> bundleData);
    }
}