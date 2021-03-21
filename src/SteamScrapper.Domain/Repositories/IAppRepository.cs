using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface IAppRepository
    {
        Task<int> RegisterUnknownAppsAsync(IEnumerable<int> appIds);
    }
}