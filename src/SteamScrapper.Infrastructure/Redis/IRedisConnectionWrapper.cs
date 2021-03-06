using StackExchange.Redis;

namespace SteamScrapper.Infrastructure.Redis
{
    public interface IRedisConnectionWrapper
    {
        IConnectionMultiplexer ConnectionMultiplexer { get; }
    }
}