using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.BundleAggregator.Commands.AggregateBundleBatch
{
    public interface IAggregateBundleBatchCommandHandler
    {
        Task<AggregateBundleBatchCommandResult> AggregateBundleBatchAsync(CancellationToken cancellationToken);
    }
}