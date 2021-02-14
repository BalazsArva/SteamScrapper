using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using SteamScrapper.Services;

namespace SteamScrapper
{
    internal class Program
    {
        private static IDatabase redisDatabase;
        private static SteamService steamService = new SteamService();

        private static async Task Main(string[] args)
        {
            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync("host.docker.internal:6379");
            redisDatabase = connectionMultiplexer.GetDatabase(2);

            /*
            var gamePage = await GamePage.CreateAsync("https://store.steampowered.com/app/378648/The_Witcher_3_Wild_Hunt__Blood_and_Wine/");
            var subPage = await SubPage.CreateAsync("https://store.steampowered.com/sub/392522");
            var bundlePage = await BundlePage.CreateAsync("https://store.steampowered.com/bundle/12231/Shadow_of_the_Tomb_Raider_Definitive_Edition/");

            var s = gamePage.GetLinksForSubs();
            */

            var steamRootUri = new Uri("https://store.steampowered.com/", UriKind.Absolute);
            var crawler = new Crawler(redisDatabase, steamService, false);

            await crawler.DiscoverSteamLinksAsync(steamRootUri);

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit...");

            var _ = Console.ReadKey();
        }
    }
}