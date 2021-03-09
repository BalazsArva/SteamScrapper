using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.AppScanner.Commands.ProcessAppBatch
{
    public interface IProcessAppBatchCommandHandler
    {
        Task<ProcessAppBatchCommandResult> ProcessAppBatchAsync(CancellationToken cancellationToken);
    }
}