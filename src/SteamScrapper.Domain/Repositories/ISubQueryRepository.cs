using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubQueryRepository
    {
        Task<int> CountUnscannedSubsAsync();

        Task<IEnumerable<long>> GetSubIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);
    }
}