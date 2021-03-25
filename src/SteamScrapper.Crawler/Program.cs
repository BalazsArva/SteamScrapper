using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SteamScrapper.Common.Providers;
using SteamScrapper.Crawler.BackgroundServices;
using SteamScrapper.Crawler.Commands.CancelReservations;
using SteamScrapper.Crawler.Commands.ExplorePage;
using SteamScrapper.Crawler.Commands.RegisterStartingAddresses;
using SteamScrapper.Crawler.Options;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Database.Context;
using SteamScrapper.Infrastructure.Database.Repositories;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.Redis;
using SteamScrapper.Infrastructure.Services;

namespace SteamScrapper.Crawler
{
    public static class Program
    {
        private const int SqlConnectionPoolSize = 32;

        public static async Task Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);
            var host = hostBuilder.Build();

            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<SqlServerOptions>(hostContext.Configuration.GetSection(SqlServerOptions.SectionName));
                    services.Configure<RedisOptions>(hostContext.Configuration.GetSection(RedisOptions.SectionName));
                    services.Configure<RegisterStartingAddressesOptions>(hostContext.Configuration.GetSection(RegisterStartingAddressesOptions.SectionName));
                    services.Configure<CrawlerAddressRegistrationOptions>(hostContext.Configuration.GetSection(CrawlerAddressRegistrationOptions.SectionName));

                    services.AddPooledDbContextFactory<SteamContext>(
                        (services, opts) => opts.UseSqlServer(services.GetRequiredService<IOptions<SqlServerOptions>>().Value.ConnectionString), SqlConnectionPoolSize);

                    services.AddSingleton<IAppWriteRepository, AppRepository>();
                    services.AddSingleton<IBundleRepository, BundleRepository>();
                    services.AddSingleton<ISubWriteRepository, SubRepository>();
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();
                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

                    services.AddSingleton<ISteamService, SteamService>();
                    services.AddSingleton<ICrawlerAddressRegistrationService, CrawlerAddressRegistrationService>();
                    services.AddSingleton<ICrawlerPrefetchService, CrawlerPrefetchService>();

                    services.AddSingleton<IRegisterStartingAddressesCommandHandler, RegisterStartingAddressesCommandHandler>();
                    services.AddSingleton<IExplorePageCommandHandler, ExplorePageCommandHandler>();
                    services.AddSingleton<ICancelReservationsCommandHandler, CancelReservationsCommandHandler>();

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();

                    services.AddHostedService<CrawlerBackgroundService>();
                });
        }
    }
}