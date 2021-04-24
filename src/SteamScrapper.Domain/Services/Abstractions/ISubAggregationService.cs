using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISubAggregationService
    {
        Task<int> CountUnscannedSubsAsync();

        Task<IEnumerable<long>> GetNextSubIdsForAggregationAsync(DateTime executionDate);
    }
}