using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.PageModels;
using SteamScrapper.Services;

namespace SteamScrapper
{
    internal class Program
    {
        private static IDatabase redisDatabase;
        private static SteamService steamService = new SteamService();

        private static async Task Main()
        {
            var developerListPage = await DeveloperListPage.CreateAsync();

            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync("host.docker.internal:6379");
            redisDatabase = connectionMultiplexer.GetDatabase(2);

            /*
            var gamePage = await GamePage.CreateAsync("https://store.steampowered.com/app/378648/The_Witcher_3_Wild_Hunt__Blood_and_Wine/");
            var subPage = await SubPage.CreateAsync("https://store.steampowered.com/sub/392522");
            var bundlePage = await BundlePage.CreateAsync("https://store.steampowered.com/bundle/12231/Shadow_of_the_Tomb_Raider_Definitive_Edition/");

            var s = gamePage.GetLinksForSubs();
            */

            var crawler = new Crawler(redisDatabase, steamService, false);
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