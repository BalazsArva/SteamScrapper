using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISteamContentRegistrationService
    {
        Task<int> RegisterUnknownAppsAsync(IEnumerable<int> appIds);

        Task<int> RegisterUnknownBundlesAsync(IEnumerable<int> bundleIds);

        Task<int> RegisterUnknownSubsAsync(IEnumerable<int> subIds);
    }
}