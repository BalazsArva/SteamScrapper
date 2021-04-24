using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SteamScrapper.Common.Hosting;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Database.Context;
using SteamScrapper.Infrastructure.Database.Repositories;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.RavenDb;
using SteamScrapper.Infrastructure.Redis;
using SteamScrapper.Infrastructure.Services;
using SteamScrapper.SubAggregator.BackgroundServices;
using SteamScrapper.SubAggregator.Commands.AggregateSubBatch;

namespace SteamScrapper.SubAggregator
{
    public static class Program
    {
        private const int SqlConnectionPoolSize = 32;

        public static async Task Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);

            await hostBuilder.Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<SqlServerOptions>(hostContext.Configuration.GetSection(SqlServerOptions.SectionName));
                    services.Configure<RedisOptions>(hostContext.Configuration.GetSection(RedisOptions.SectionName));
                    services.Configure<RavenDbOptions>(hostContext.Configuration.GetSection(RavenDbOptions.SectionName));

                    services.AddPooledDbContextFactory<SteamContext>(
                        (services, opts) => opts.UseSqlServer(services.GetRequiredService<IOptions<SqlServerOptions>>().Value.ConnectionString), SqlConnectionPoolSize);

                    services.AddSingleton<SubRepository>();
                    services.AddSingleton<ISubQueryRepository>(services => services.GetRequiredService<SubRepository>());
                    services.AddSingleton<ISubWriteRepository>(services => services.GetRequiredService<SubRepository>());

                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddSingleton<ISubAggregationService, SubAggregationService>();

                    services.AddSingleton<IAggregateSubBatchCommandHandler, AggregateSubBatchCommandHandler>();

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddSingleton<IDocumentStoreWrapper, DocumentStoreWrapper>();

                    services
                        .AddHealthChecks()
                        .AddCheck<RavenDbHealthCheck>("RavenDB", HealthStatus.Unhealthy, new[] { "RavenDB", "Database" })
                        .AddCheck<SteamContextHealthCheck>("SQL Server", HealthStatus.Unhealthy, new[] { "SQL Server", "Database" })
                        .AddCheck<RedisHealthCheck>("Redis", HealthStatus.Unhealthy, new[] { "Redis" });

                    services.AddHostedService<AggregateSubsBackgroundService>();
                    services.AddHostedService<HealthCheckBackgroundService>();
                });
        }
    }
}