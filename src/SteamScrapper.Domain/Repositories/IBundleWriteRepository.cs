using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface IBundleWriteRepository
    {
        Task<int> RegisterUnknownBundlesAsync(IEnumerable<long> bundleIds);
    }
}