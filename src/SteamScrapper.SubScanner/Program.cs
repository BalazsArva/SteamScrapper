using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SteamScrapper.Common.HealthCheck;
using SteamScrapper.Common.Hosting;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Database.Context;
using SteamScrapper.Infrastructure.Database.Repositories;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.Redis;
using SteamScrapper.Infrastructure.Services;
using SteamScrapper.SubScanner.BackgroundServices;
using SteamScrapper.SubScanner.Commands.ScanSubBatch;
using SteamScrapper.SubScanner.Options;

namespace SteamScrapper.SubScanner
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
                    services.Configure<ScanSubBatchOptions>(hostContext.Configuration.GetSection(ScanSubBatchOptions.SectionName));

                    services.AddPooledDbContextFactory<SteamContext>(
                        (services, opts) => opts.UseSqlServer(services.GetRequiredService<IOptions<SqlServerOptions>>().Value.ConnectionString), SqlConnectionPoolSize);

                    services.AddSingleton<SubRepository>();
                    services.AddSingleton<ISubQueryRepository>(services => services.GetRequiredService<SubRepository>());
                    services.AddSingleton<ISubWriteRepository>(services => services.GetRequiredService<SubRepository>());

                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();
                    services.AddSingleton<ISteamService, SteamService>();
                    services.AddSingleton<ISubScanningService, SubScanningService>();

                    services.AddSingleton<IScanSubBatchCommandHandler, ScanSubBatchCommandHandler>();

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();

                    services.AddSingleton<IHealthCheckable>(services => services.GetRequiredService<IRedisConnectionWrapper>());
                    services.AddSingleton<IHealthCheckable, SteamContextHealthChecker>();

                    services.AddHostedService<ScanSubsBackgroundService>();
                    services.AddHostedService<HealthCheckBackgroundService>();
                });
        }
    }
}