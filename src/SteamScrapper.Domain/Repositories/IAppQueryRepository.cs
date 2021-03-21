using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface IAppQueryRepository
    {
        Task<int> CountUnscannedAppsAsync();
    }
}