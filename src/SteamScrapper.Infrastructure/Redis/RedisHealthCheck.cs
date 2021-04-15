using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace SteamScrapper.Infrastructure.Redis
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IRedisConnectionWrapper redisConnectionWrapper;

        public RedisHealthCheck(IRedisConnectionWrapper redisConnectionWrapper)
        {
            this.redisConnectionWrapper = redisConnectionWrapper ?? throw new ArgumentNullException(nameof(redisConnectionWrapper));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _ = await redisConnectionWrapper.ConnectionMultiplexer.GetDatabase().PingAsync(CommandFlags.DemandMaster);

                return HealthCheckResult.Healthy("Successfully checked Redis master node health.");
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy("Failed to get ping response from Redis master node.", e);
            }
        }
    }
}