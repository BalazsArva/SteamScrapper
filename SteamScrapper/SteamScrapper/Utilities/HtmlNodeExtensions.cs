using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace SteamScrapper.Utilities
{
    public static class HtmlNodeExtensions
    {
        public static IEnumerable<HtmlNode> GetDescendantsByClass(this HtmlNode htmlNode, string @class)
        {
            if (htmlNode is null)
            {
                throw new ArgumentNullException(nameof(htmlNode));
            }

            if (string.IsNullOrWhiteSpace(@class))
            {
                throw new ArgumentException($"'{nameof(@class)}' cannot be null or whitespace", nameof(@class));
            }

            return htmlNode
                .Descendants()
                .Where(node => node.GetClasses().Contains(@class, StringComparer.Ordinal));
        }
    }
}