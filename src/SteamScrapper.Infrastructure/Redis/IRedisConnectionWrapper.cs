using StackExchange.Redis;
using SteamScrapper.Common.HealthCheck;

namespace SteamScrapper.Infrastructure.Redis
{
    public interface IRedisConnectionWrapper : IHealthCheckable
    {
        IConnectionMultiplexer ConnectionMultiplexer { get; }
    }
}