using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.SubScanner.Commands.ScanSubBatch
{
    public interface IScanSubBatchCommandHandler
    {
        Task<ScanSubBatchCommandResult> ScanSubBatchAsync(CancellationToken cancellationToken);
    }
}