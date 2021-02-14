using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace SteamScrapper.Extensions
{
    public static class HtmlExtensions
    {
        private const int DefualtProcessingQueueLength = ushort.MaxValue;

        public static IEnumerable<Uri> GetSteamLinks(this HtmlNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var processingQueue = new Queue<HtmlNode>(DefualtProcessingQueueLength);

            processingQueue.Enqueue(node);

            while (processingQueue.Count != 0)
            {
                var nodeToProcess = processingQueue.Dequeue();

                foreach (var childNode in nodeToProcess.ChildNodes)
                {
                    processingQueue.Enqueue(childNode);
                }

                if (nodeToProcess.Name != "a")
                {
                    continue;
                }

                var hrefAttribute = nodeToProcess.Attributes.FirstOrDefault(a => a.Name == "href");

                if (
                    string.IsNullOrWhiteSpace(hrefAttribute?.Value) ||
                    !hrefAttribute.Value.StartsWith("https://store.steampowered.com") ||
                    hrefAttribute.Value.StartsWith("https://store.steampowered.com/app/") ||
                    hrefAttribute.Value.StartsWith("https://store.steampowered.com/sub/") ||
                    hrefAttribute.Value.StartsWith("https://store.steampowered.com/recommended/morelike/app/"))
                {
                    continue;
                }

                if (Uri.TryCreate(hrefAttribute.Value, UriKind.Absolute, out var uri))
                {
                    // Drop any query string values
                    var normalizedSegments = string.Concat(uri.Segments);

                    yield return new UriBuilder(uri.Scheme, uri.Host, uri.Port, normalizedSegments).Uri;
                }
            }
        }

        public static IEnumerable<Uri> GetAppLinks(this HtmlNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var processingQueue = new Queue<HtmlNode>(DefualtProcessingQueueLength);

            processingQueue.Enqueue(node);

            while (processingQueue.Count != 0)
            {
                var nodeToProcess = processingQueue.Dequeue();

                var appLinks = nodeToProcess.Attributes.Where(a => a.Name == "href" && a.Value.StartsWith("https://store.steampowered.com/app/"));
                foreach (var appLink in appLinks)
                {
                    if (Uri.TryCreate(appLink.Value, UriKind.Absolute, out var uri))
                    {
                        var segments = uri.Segments;
                        if ((segments.Length == 3 || segments.Length == 4) && segments[0] == "/" && segments[1] == "app/")
                        {
                            // Apps links can be the following format:
                            // - https://store.steampowered.com/app/292030/
                            // - https://store.steampowered.com/app/292030/The_Witcher_3_Wild_Hunt/
                            // We want to detect the links which refer to the same, so we drop the game name and make use of the Id only.
                            var normalizedSegments = string.Concat(segments.Skip(1).Take(2));
                            var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, normalizedSegments);

                            yield return builder.Uri;
                        }
                    }
                }

                foreach (var childNode in nodeToProcess.ChildNodes)
                {
                    processingQueue.Enqueue(childNode);
                }
            }
        }
    }
}