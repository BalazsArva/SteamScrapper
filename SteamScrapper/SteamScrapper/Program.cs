using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Services;
using SteamScrapper.Utilities.Factories;

namespace SteamScrapper
{
    internal class Program
    {
        private static IDatabase redisDatabase;
        private static SteamPageFactory steamPageFactory;

        private static async Task Main()
        {
            steamPageFactory = new SteamPageFactory(new SteamService());

            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync("host.docker.internal:6379");
            redisDatabase = connectionMultiplexer.GetDatabase(2);

            var crawler = new Crawler(redisDatabase, steamPageFactory, false);
            var startingUris = new[]
            {
                new Uri("https://store.steampowered.com/", UriKind.Absolute),
                new Uri("https://store.steampowered.com/developer/", UriKind.Absolute),
                new Uri("https://store.steampowered.com/publisher/", UriKind.Absolute),
            };

            await crawler.DiscoverSteamLinksAsync(startingUris);

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit...");

            var _ = Console.ReadKey();
        }
    }
}