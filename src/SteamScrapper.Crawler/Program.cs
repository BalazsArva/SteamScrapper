using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SteamScrapper.Crawler.BackgroundServices;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.Redis;
using SteamScrapper.Infrastructure.Services;

namespace SteamScrapper.Crawler
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
                    services.Configure<CrawlerAddressRegistrationOptions>(hostContext.Configuration.GetSection(CrawlerAddressRegistrationOptions.SectionName));

                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();

                    services.AddSingleton<ISteamContentRegistrationService, SteamContentRegistrationService>();
                    services.AddSingleton<ISteamService, SteamService>();
                    services.AddSingleton<ICrawlerAddressRegistrationService, CrawlerAddressRegistrationService>();
                    services.AddSingleton<ICrawlerPrefetchService, CrawlerPrefetchService>();

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddSingleton(sqlConnection);

                    services.AddHostedService<CrawlerBackgroundService>();
                });
        }
    }
}