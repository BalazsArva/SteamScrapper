﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISubScanningService
    {
        Task<IEnumerable<long>> GetNextSubIdsForScanningAsync();

        Task<int> CountUnscannedSubsAsync();
    }
}