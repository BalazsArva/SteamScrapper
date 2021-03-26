using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Contracts;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubWriteRepository
    {
        Task<int> RegisterUnknownSubsAsync(IEnumerable<long> subIds);

        Task UpdateSubsAsync(IEnumerable<SubData> subData);
    }
}