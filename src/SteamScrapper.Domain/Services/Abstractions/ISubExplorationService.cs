using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISubExplorationService
    {
        Task<IEnumerable<int>> GetNextSubIdsForExplorationAsync(DateTime executionDate);

        Task UpdateSubsAsync(IEnumerable<SubData> subData);
    }
}