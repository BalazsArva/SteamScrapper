using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubQueryRepository
    {
        // TODO: Add service pass-through
        Task<int> CountUnscannedSubsAsync();

        Task<int> CountUnscannedSubsFromAsync(DateTime from);

        Task<IEnumerable<long>> GetSubIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);

        Task<IEnumerable<long>> GetSubIdsNotAggregatedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);
    }
}