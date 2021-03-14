﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISubScanningService
    {
        Task<int> GetCountOfUnscannedSubsAsync();

        Task<IEnumerable<int>> GetNextSubIdsForScanningAsync(DateTime executionDate);

        Task UpdateSubsAsync(IEnumerable<SubData> subData);
    }
}