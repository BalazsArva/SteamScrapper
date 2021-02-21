using System;
using HtmlAgilityPack;

namespace SteamScrapper.PageModels
{
    public class GamePage : SteamPage
    {
        // TODO: Implement other stuff (price, title extraction, etc.)
        public GamePage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml)
        {
        }
    }
}