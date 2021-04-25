using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISubAggregationService
    {
        Task ConfirmAggregationAsync(IEnumerable<long> subIds);

        Task<int> CountUnaggregatedSubsAsync();

        Task<IEnumerable<long>> GetNextSubIdsForAggregationAsync();
    }
}