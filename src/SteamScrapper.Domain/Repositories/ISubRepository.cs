using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubRepository
    {
        Task<int> RegisterUnknownSubsAsync(IEnumerable<int> subIds);
    }
}