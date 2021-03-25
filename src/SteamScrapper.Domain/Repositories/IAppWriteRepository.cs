﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.Domain.Repositories
{
    public interface IAppWriteRepository
    {
        Task<int> RegisterUnknownAppsAsync(IEnumerable<long> appIds);

        Task UpdateAppsAsync(IEnumerable<AppData> appData);
    }
}