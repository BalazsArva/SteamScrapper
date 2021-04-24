using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Repositories.Models;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubWriteRepository
    {
        Task AddSubAggregationsAsync(IEnumerable<long> subIds, DateTime performedAt);

        Task<int> RegisterUnknownSubsAsync(IEnumerable<long> subIds);

        Task UpdateSubsAsync(IEnumerable<Sub> subData);
    }
}