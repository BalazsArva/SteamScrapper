using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SteamScrapper.BundleScanner.BackgroundServices;
using SteamScrapper.BundleScanner.Commands.ScanBundleBatch;
using SteamScrapper.BundleScanner.Options;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Database.Context;
using SteamScrapper.Infrastructure.Database.Repositories;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.Redis;
using SteamScrapper.Infrastructure.Services;

namespace SteamScrapper.BundleScanner
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
                    services.Configure<ScanBundleBatchOptions>(hostContext.Configuration.GetSection(ScanBundleBatchOptions.SectionName));

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();

                    services.AddPooledDbContextFactory<SteamContext>(
                        (services, opts) => opts.UseSqlServer(services.GetRequiredService<IOptions<SqlServerOptions>>().Value.ConnectionString), SqlConnectionPoolSize);

                    services.AddSingleton<BundleRepository>();
                    services.AddSingleton<IBundleQueryRepository>(services => services.GetRequiredService<BundleRepository>());
                    services.AddSingleton<IBundleWriteRepository>(services => services.GetRequiredService<BundleRepository>());

                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();
                    services.AddSingleton<ISteamService, SteamService>();
                    services.AddSingleton<IBundleScanningService, BundleScanningService>();

                    services.AddSingleton<IScanBundleBatchCommandHandler, ScanBundleBatchCommandHandler>();
                    services.AddHostedService<ScanBundlesBackgroundService>();
                });
        }
    }
}