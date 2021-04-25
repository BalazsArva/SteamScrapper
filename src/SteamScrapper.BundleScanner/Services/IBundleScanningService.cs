using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.BundleScanner.Services
{
    public interface IBundleScanningService
    {
        Task<IEnumerable<long>> GetNextBundleIdsForScanningAsync();

        Task<int> CountUnscannedBundlesAsync();
    }
}