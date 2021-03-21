﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface IAppScanningService
    {
        Task<IEnumerable<long>> GetNextAppIdsForScanningAsync(DateTime executionDate);

        Task UpdateAppsAsync(IEnumerable<AppData> appData);
    }
}