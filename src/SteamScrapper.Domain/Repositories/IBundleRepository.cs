using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface IBundleRepository
    {
        Task<int> RegisterUnknownBundlesAsync(IEnumerable<long> bundleIds);
    }
}