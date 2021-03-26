using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface IBundleQueryRepository
    {
        Task<int> CountUnscannedBundlesAsync();
    }
}