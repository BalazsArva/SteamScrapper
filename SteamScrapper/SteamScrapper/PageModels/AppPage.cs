﻿using System;
using HtmlAgilityPack;

namespace SteamScrapper.PageModels
{
    public class AppPage : SteamPage
    {
        // TODO: Implement other stuff (price, title extraction, etc.)
        public AppPage(Uri address, HtmlDocument pageHtml)
            : base(address, pageHtml)
        {
        }
    }
}