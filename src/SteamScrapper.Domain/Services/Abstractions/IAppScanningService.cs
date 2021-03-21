using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface IAppScanningService
    {
        Task<IEnumerable<long>> GetNextAppIdsForScanningAsync(DateTime executionDate);
    }
}