using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.Common.HealthCheck
{
    public interface IHealthCheckable
    {
        Task<HealthCheckResult> GetHealthAsync(CancellationToken cancellationToken);
    }
}