using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using SteamScrapper.Crawler.BackgroundServices;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Services;

namespace SteamScrapper.Crawler
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = await CreateHostBuilderAsync(args);

            await hostBuilder.Build().RunAsync();
        }

        public static async Task<IHostBuilder> CreateHostBuilderAsync(string[] args)
        {
            // TODO: Improve these dependencies
            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync("host.docker.internal:6379");
            var sqlConnection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDb;Initial Catalog=SteamScrapper;Integrated Security=true");

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();

                    services.AddSingleton<ISteamContentRegistrationService, SteamContentRegistrationService>();
                    services.AddSingleton<ISteamService, SteamService>();
                    services.AddSingleton<ICrawlerAddressRegistrationService, CrawlerAddressRegistrationService>();

                    services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);
                    services.AddSingleton(sqlConnection);

                    services.AddHostedService<CrawlerBackgroundService>();
                });
        }
    }
}