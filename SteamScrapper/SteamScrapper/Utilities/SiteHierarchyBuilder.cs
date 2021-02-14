using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SteamScrapper.Utilities
{
    public static class SiteHierarchyBuilder
    {
        private static readonly HtmlWeb Downloader = new HtmlWeb();

        public static async Task BuildSiteHierarchyAsync()
        {
            var root = await Downloader.LoadFromWebAsync("https://store.steampowered.com/");
        }
    }

    public class SiteHierarchyNode
    {
        public string Segment { get; init; }
    }
}