using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.BundleAggregator.Services
{
    public interface IBundleAggregationService
    {
        Task ConfirmAggregationAsync(IEnumerable<long> bundleIds);

        Task<int> CountUnaggregatedBundlesAsync();

        Task<IEnumerable<long>> GetNextBundleIdsForAggregationAsync();
    }
}