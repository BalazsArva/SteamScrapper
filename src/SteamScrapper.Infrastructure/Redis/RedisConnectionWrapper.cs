using System;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using SteamScrapper.Infrastructure.Options;

namespace SteamScrapper.Infrastructure.Redis
{
    public class RedisConnectionWrapper : IRedisConnectionWrapper
    {
        private readonly Lazy<IConnectionMultiplexer> connectionMultiplexerLazy;

        public RedisConnectionWrapper(IOptions<RedisOptions> redisOptions)
        {
            if (redisOptions is null)
            {
                throw new ArgumentNullException(nameof(redisOptions));
            }

            if (string.IsNullOrWhiteSpace(redisOptions.Value?.ConnectionString))
            {
                throw new ArgumentException("The provided configuration object does not contain a valid connection string.", nameof(redisOptions));
            }

            connectionMultiplexerLazy = new Lazy<IConnectionMultiplexer>(() => StackExchange.Redis.ConnectionMultiplexer.Connect(redisOptions.Value.ConnectionString));
        }

        public IConnectionMultiplexer ConnectionMultiplexer => connectionMultiplexerLazy.Value;
    }
}