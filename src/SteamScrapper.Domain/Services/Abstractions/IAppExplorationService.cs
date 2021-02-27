using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface IAppExplorationService
    {
        Task<IEnumerable<int>> GetNextAppIdsAsync(DateTime executionDate);

        Task UpdateAppsAsync(Dictionary<int, string> idsWithTitles);
    }
}