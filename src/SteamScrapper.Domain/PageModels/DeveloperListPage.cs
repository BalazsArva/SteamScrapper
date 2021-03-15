using System;
using HtmlAgilityPack;

namespace SteamScrapper.Domain.PageModels
{
    public class DeveloperListPage : SteamPage
    {
        public DeveloperListPage(Uri baseAddress, HtmlDocument htmlDocument)
            : base(baseAddress, htmlDocument)
        {
        }
    }
}