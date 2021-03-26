using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface IBundleScanningService
    {
        Task<IEnumerable<long>> GetNextBundleIdsForScanningAsync(DateTime executionDate);
    }
}