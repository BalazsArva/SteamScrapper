using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Repositories.Models;

namespace SteamScrapper.Domain.Repositories
{
    public interface IBundleWriteRepository
    {
        Task<int> RegisterUnknownBundlesAsync(IEnumerable<long> bundleIds);

        Task UpdateBundlesAsync(IEnumerable<Bundle> bundleData);
    }
}