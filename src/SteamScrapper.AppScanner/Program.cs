using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SteamScrapper.AppScanner.BackgroundServices;
using SteamScrapper.AppScanner.Commands.ScanAppBatch;
using SteamScrapper.AppScanner.Options;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Database.Context;
using SteamScrapper.Infrastructure.Database.Repositories;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.Redis;
using SteamScrapper.Infrastructure.Services;

namespace SteamScrapper.AppScanner
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
                    services.Configure<ScanAppBatchOptions>(hostContext.Configuration.GetSection(ScanAppBatchOptions.SectionName));

                    services.AddPooledDbContextFactory<SteamContext>(
                        (services, opts) => opts.UseSqlServer(services.GetRequiredService<IOptions<SqlServerOptions>>().Value.ConnectionString), SqlConnectionPoolSize);

                    services.AddSingleton<AppRepository>();
                    services.AddSingleton<IAppQueryRepository>(services => services.GetRequiredService<AppRepository>());
                    services.AddSingleton<IAppWriteRepository>(services => services.GetRequiredService<AppRepository>());

                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();
                    services.AddSingleton<ISteamService, SteamService>();

                    services.AddSingleton<IAppScanningService, AppScanningService>();

                    services.AddSingleton<IScanAppBatchCommandHandler, ScanAppBatchCommandHandler>();

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddSingleton(services =>
                    {
                        var sqlConnectionString = services.GetRequiredService<IOptions<SqlServerOptions>>().Value.ConnectionString;

                        return new SqlConnection(sqlConnectionString);
                    });

                    services.AddHostedService<ScanAppsBackgroundService>();
                });
        }
    }
}