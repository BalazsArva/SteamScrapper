using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.Domain.Repositories
{
    public interface IBundleWriteRepository
    {
        Task<int> RegisterUnknownBundlesAsync(IEnumerable<long> bundleIds);

        Task UpdateBundlesAsync(IEnumerable<BundleData> bundleData);
    }
}