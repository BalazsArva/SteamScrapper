using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Services;
using SteamScrapper.SubExplorer.BackgroundServices;
using SteamScrapper.SubExplorer.Commands.ProcessSubBatch;

namespace SteamScrapper.SubExplorer
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = await CreateHostBuilderAsync(args);

            await hostBuilder.Build().RunAsync();
        }

        private static async Task<IHostBuilder> CreateHostBuilderAsync(string[] args)
        {
            // TODO: Improve these dependencies
            var redisConfigurationOptions = ConfigurationOptions.Parse("host.docker.internal:6379");
            redisConfigurationOptions.ClientName = "SteamScrapper.SubExplorer";
            redisConfigurationOptions.ConnectTimeout = 60 * 1000;
            redisConfigurationOptions.AsyncTimeout = 10 * 1000;
            redisConfigurationOptions.SyncTimeout = 10 * 1000;
            redisConfigurationOptions.AbortOnConnectFail = false;
            redisConfigurationOptions.ConnectRetry = 5;

            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConfigurationOptions);
            var sqlConnection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDb;Initial Catalog=SteamScrapper;Integrated Security=true");

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();
                    services.AddSingleton<ISteamService, SteamService>();

                    services.AddSingleton<ISubExplorationService, SubExplorationService>();

                    services.AddSingleton<IProcessSubBatchCommandHandler, ProcessSubBatchCommandHandler>();

                    services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);
                    services.AddSingleton(sqlConnection);

                    services.AddHostedService<ProcessSubsBackgroundService>();
                });
        }
    }
}