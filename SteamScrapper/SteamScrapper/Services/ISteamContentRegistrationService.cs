using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Services
{
    public interface ISteamContentRegistrationService
    {
        Task RegisterUnknownAppsAsync(IEnumerable<int> appIds);
        Task RegisterUnknownBundlesAsync(IEnumerable<int> bundleIds);
        Task RegisterUnknownSubsAsync(IEnumerable<int> subIds);
    }
}