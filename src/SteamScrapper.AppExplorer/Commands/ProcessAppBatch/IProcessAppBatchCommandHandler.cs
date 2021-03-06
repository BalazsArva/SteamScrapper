using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.AppExplorer.Commands.ProcessAppBatch
{
    public interface IProcessAppBatchCommandHandler
    {
        Task<ProcessAppBatchCommandResult> ProcessAppBatchAsync(CancellationToken cancellationToken);
    }
}