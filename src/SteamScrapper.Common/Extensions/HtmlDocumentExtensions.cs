using System;
using System.Linq;
using HtmlAgilityPack;

namespace SteamScrapper.Common.Extensions
{
    public static class HtmlDocumentExtensions
    {
        public static HtmlNode GetDescendantById(this HtmlDocument htmlDocument, string id)
        {
            if (htmlDocument is null)
            {
                throw new ArgumentNullException(nameof(htmlDocument));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"'{nameof(id)}' cannot be null or whitespace", nameof(id));
            }

            return htmlDocument
                .DocumentNode
                .Descendants()
                .FirstOrDefault(node => string.Equals(node.Id, id, StringComparison.Ordinal)); ;
        }
    }
}