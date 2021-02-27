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
                .DocumentNode
                .Descendants()
                .FirstOrDefault(node => string.Equals(node.Id, id, StringComparison.Ordinal)); ;
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

        public static IDictionary<string, IEnumerable<HtmlNode>> GetDescendantsByNames(this HtmlDocument doc, params string[] elementNames)
        {
            if (doc is null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            return doc.DocumentNode.GetDescendantsByNames(elementNames);
        }

        public static IDictionary<string, IEnumerable<HtmlNode>> GetDescendantsByNames(this HtmlNode htmlNode, params string[] elementNames)
        {
            if (htmlNode is null)
            {
                throw new ArgumentNullException(nameof(htmlNode));
            }

            if (elementNames is null)
            {
                throw new ArgumentNullException(nameof(elementNames));
            }

            if (elementNames.Length == 0)
            {
                throw new ArgumentException("At least one element name must be provided.", nameof(elementNames));
            }

            var result = new Dictionary<string, List<HtmlNode>>(elementNames.Length, StringComparer.Ordinal);

            for (var i = 0; i < elementNames.Length; ++i)
            {
                var elementName = elementNames[i];

                if (string.IsNullOrWhiteSpace(elementName))
                {
                    throw new ArgumentException("The element name cannot be null, empty or whitespace-only.", nameof(elementNames));
                }

                result[elementName] = new List<HtmlNode>(256);
            }

            var processingQueue = new Queue<HtmlNode>(1024);

            processingQueue.Enqueue(htmlNode);

            while (processingQueue.Count != 0)
            {
                var itemToProcess = processingQueue.Dequeue();

                if (result.ContainsKey(itemToProcess.Name))
                {
                    result[itemToProcess.Name].Add(itemToProcess);
                }

                processingQueue.EnqueueRange(itemToProcess.ChildNodes);
            }

            return result.ToDictionary(x => x.Key, x => (IEnumerable<HtmlNode>)x.Value);
        }
    }
}