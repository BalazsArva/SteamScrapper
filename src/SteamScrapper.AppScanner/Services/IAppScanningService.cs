using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.AppScanner.Services
{
    public interface IAppScanningService
    {
        Task<IEnumerable<long>> GetNextAppIdsForScanningAsync();

        Task<int> CountUnscannedAppsAsync();
    }
}