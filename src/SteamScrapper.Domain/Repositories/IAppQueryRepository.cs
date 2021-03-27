using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface IAppQueryRepository
    {
        Task<int> CountUnscannedAppsAsync();

        Task<IEnumerable<long>> GetAppIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);
    }
}