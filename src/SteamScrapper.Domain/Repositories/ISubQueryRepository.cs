using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubQueryRepository
    {
        Task<int> CountUnscannedSubsAsync();
    }
}