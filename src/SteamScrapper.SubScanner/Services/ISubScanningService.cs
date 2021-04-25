using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.SubScanner.Services
{
    public interface ISubScanningService
    {
        Task<IEnumerable<long>> GetNextSubIdsForScanningAsync();

        Task<int> CountUnscannedSubsAsync();
    }
}