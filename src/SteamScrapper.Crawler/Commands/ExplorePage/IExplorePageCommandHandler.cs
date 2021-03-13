using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.Crawler.Commands.ExplorePage
{
    public interface IExplorePageCommandHandler
    {
        Task<ExplorePageCommandResult> ExplorePageAsync(CancellationToken cancellationToken);
    }
}