using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.AppScanner.Commands.ScanAppBatch
{
    public interface IScanAppBatchCommandHandler
    {
        Task<ScanAppBatchCommandResult> ScanAppBatchAsync(CancellationToken cancellationToken);
    }
}