using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Repositories.Models;

namespace SteamScrapper.Domain.Repositories
{
    public interface IBundleQueryRepository
    {
        Task<int> CountUnscannedBundlesFromAsync(DateTime from);

        Task<int> CountUnaggregatedBundlesFromAsync(DateTime from);

        Task<IEnumerable<long>> GetBundleIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);

        Task<IEnumerable<long>> GetBundleIdsNotAggregatedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);

        Task<Bundle> GetBundleBasicDetailsByIdAsync(long bundleId);

        Task<IEnumerable<Price>> GetBundlePriceHistoryByIdAsync(long bundleId);
    }
}