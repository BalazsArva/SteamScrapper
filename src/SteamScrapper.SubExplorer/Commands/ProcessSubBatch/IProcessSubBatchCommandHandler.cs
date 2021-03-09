using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.SubScanner.Commands.ProcessSubBatch
{
    public interface IProcessSubBatchCommandHandler
    {
        Task<ProcessSubBatchCommandResult> ProcessSubBatchAsync(CancellationToken cancellationToken);
    }
}