using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubWriteRepository
    {
        Task<int> RegisterUnknownSubsAsync(IEnumerable<long> subIds);
    }
}