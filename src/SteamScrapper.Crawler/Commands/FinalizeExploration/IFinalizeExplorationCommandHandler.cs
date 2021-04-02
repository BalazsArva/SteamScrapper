using System.Threading.Tasks;

namespace SteamScrapper.Crawler.Commands.FinalizeExploration
{
    public interface IFinalizeExplorationCommandHandler
    {
        Task FinalizeExplorationAsync();
    }
}