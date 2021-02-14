using System;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SteamScrapper.PageModels
{
    public class GamePage : SteamPage
    {
        private static readonly HtmlWeb Downloader = new HtmlWeb();

        public GamePage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml)
        {
        }

        public static async Task<GamePage> CreateAsync(string address)
        {
            var doc = await Downloader.LoadFromWebAsync(address);

            return new GamePage(new Uri(address), doc);
        }
    }
}