using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface IAppQueryRepository
    {
        Task<int> CountUnscannedAppsAsync(DateTime from);

        Task<IEnumerable<long>> GetAppIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);
    }
}