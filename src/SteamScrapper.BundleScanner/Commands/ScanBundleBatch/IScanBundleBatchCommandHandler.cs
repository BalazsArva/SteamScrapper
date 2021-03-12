using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.BundleScanner.Commands.ScanBundleBatch
{
    public interface IScanBundleBatchCommandHandler
    {
        Task<ScanBundleBatchCommandResult> ScanBundleBatchAsync(CancellationToken cancellationToken);
    }
}