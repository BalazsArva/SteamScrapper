using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface IBundleScanningService
    {
        Task<int> GetCountOfUnscannedBundlesAsync();

        Task<IEnumerable<int>> GetNextBundleIdsForScanningAsync(DateTime executionDate);

        Task UpdateBundlesAsync(IEnumerable<BundleData> bundleData);
    }
}