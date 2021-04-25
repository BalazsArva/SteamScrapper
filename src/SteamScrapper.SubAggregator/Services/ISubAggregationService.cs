using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.SubAggregator.Services
{
    public interface ISubAggregationService
    {
        Task ConfirmAggregationAsync(IEnumerable<long> subIds);

        Task<int> CountUnaggregatedSubsAsync();

        Task<IEnumerable<long>> GetNextSubIdsForAggregationAsync();
    }
}