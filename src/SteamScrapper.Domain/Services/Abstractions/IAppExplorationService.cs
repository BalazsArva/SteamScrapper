using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface IAppExplorationService
    {
        Task<IEnumerable<int>> GetNextAppIdsAsync(DateTime executionDate);

        Task UpdateAppsAsync(IEnumerable<AppData> appData);
    }
}