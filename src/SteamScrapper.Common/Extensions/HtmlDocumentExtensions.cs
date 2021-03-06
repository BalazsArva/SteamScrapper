using System;
using System.Collections.Generic;
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
                .FastEnumerateDescendants()
                .FirstOrDefault(node => string.Equals(node.Id, id, StringComparison.Ordinal));
        }

        public static IEnumerable<HtmlNode> FastEnumerateDescendants(this HtmlDocument doc)
        {
            if (doc is null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            var processingQueue = new Queue<HtmlNode>(1024);
            var result = new List<HtmlNode>(4096);

            processingQueue.Enqueue(doc.DocumentNode);

            while (processingQueue.Count != 0)
            {
                var itemToProcess = processingQueue.Dequeue();

                result.Add(itemToProcess);

                processingQueue.EnqueueRange(itemToProcess.ChildNodes);
            }

            return result;
        }

        public static IEnumerable<HtmlNode> FastEnumerateDescendantsByName(this HtmlDocument doc, string nodeName)
        {
            if (doc is null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            var processingQueue = new Queue<HtmlNode>(1024);
            var result = new List<HtmlNode>(4096);

            processingQueue.Enqueue(doc.DocumentNode);

            while (processingQueue.Count != 0)
            {
                var itemToProcess = processingQueue.Dequeue();

                if (itemToProcess.Name == nodeName)
                {
                    result.Add(itemToProcess);
                }

                processingQueue.EnqueueRange(itemToProcess.ChildNodes);
            }

            return result;
        }

        public static Dictionary<string, IEnumerable<HtmlNode>> GetDescendantsByNames(this HtmlDocument doc, params string[] elementNames)
        {
            if (doc is null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            return doc.DocumentNode.GetDescendantsByNames(elementNames);
        }
    }
}