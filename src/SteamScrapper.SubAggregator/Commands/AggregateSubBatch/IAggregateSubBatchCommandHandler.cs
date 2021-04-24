using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.SubAggregator.Commands.AggregateSubBatch
{
    public interface IAggregateSubBatchCommandHandler
    {
        Task<AggregateSubBatchCommandResult> AggregateSubBatchAsync(CancellationToken cancellationToken);
    }
}