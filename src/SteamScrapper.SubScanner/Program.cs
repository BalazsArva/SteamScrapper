using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Services.Abstractions;
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
        public static async Task Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);

            await hostBuilder.Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            // TODO: Improve these dependencies
            var sqlConnection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDb;Initial Catalog=SteamScrapper;Integrated Security=true");

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<RedisOptions>(hostContext.Configuration.GetSection(RedisOptions.SectionName));
                    services.Configure<ScanSubBatchOptions>(hostContext.Configuration.GetSection(ScanSubBatchOptions.SectionName));

                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();
                    services.AddSingleton<ISteamService, SteamService>();

                    services.AddSingleton<ISubScanningService, SubExplorationService>();

                    services.AddSingleton<IScanSubBatchCommandHandler, ScanSubBatchCommandHandler>();

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddSingleton(sqlConnection);

                    services.AddHostedService<ScanSubsBackgroundService>();
                });
        }
    }
}