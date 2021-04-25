using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Repositories.Models;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubQueryRepository
    {
        Task<int> CountUnscannedSubsFromAsync(DateTime from);

        Task<int> CountUnaggregatedSubsFromAsync(DateTime from);

        Task<Sub> GetSubBasicDetailsByIdAsync(long subId);

        Task<IEnumerable<Price>> GetSubPriceHistoryByIdAsync(long subId);

        Task<IEnumerable<long>> GetSubIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);

        Task<IEnumerable<long>> GetSubIdsNotAggregatedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);
    }
}