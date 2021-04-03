using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using SteamScrapper.Common.HealthCheck;
using SteamScrapper.Infrastructure.Options;

namespace SteamScrapper.Infrastructure.Redis
{
    public class RedisConnectionWrapper : IRedisConnectionWrapper
    {
        private static readonly string ReporterName = typeof(RedisConnectionWrapper).FullName;

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

        public async Task<HealthCheckResult> GetHealthAsync(CancellationToken cancellationToken)
        {
            try
            {
                _ = await ConnectionMultiplexer.GetDatabase().PingAsync(CommandFlags.DemandMaster);

                return new(true, ReporterName);
            }
            catch (Exception e)
            {
                return new HealthCheckResult(false, ReporterName, "Failed to get ping response from Redis master node.", e);
            }
        }
    }
}