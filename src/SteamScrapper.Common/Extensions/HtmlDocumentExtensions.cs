using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace SteamScrapper.Common.Extensions
{
    public static class HtmlDocumentExtensions
    {
        private const int ProcessingQueueDefaultCapacity = 1024;
        private const int ResultListDefaultCapacity = 4096;

        public static IEnumerable<HtmlNode> FilterDescendants(this HtmlDocument htmlDocument, Predicate<HtmlNode> filter)
        {
            if (htmlDocument is null)
            {
                throw new ArgumentNullException(nameof(htmlDocument));
            }

            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            var processingQueue = new Queue<HtmlNode>(ProcessingQueueDefaultCapacity);
            var result = new List<HtmlNode>(ResultListDefaultCapacity);

            processingQueue.Enqueue(htmlDocument.DocumentNode);

            while (processingQueue.Count != 0)
            {
                var itemToProcess = processingQueue.Dequeue();

                if (filter(itemToProcess))
                {
                    result.Add(itemToProcess);
                }

                processingQueue.EnqueueRange(itemToProcess.ChildNodes);
            }

            return result;
        }

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

            return FilterDescendants(htmlDocument, node => string.Equals(node.Id, id, StringComparison.Ordinal)).FirstOrDefault();
        }

        public static IEnumerable<HtmlNode> FastEnumerateDescendants(this HtmlDocument doc)
        {
            if (doc is null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            var processingQueue = new Queue<HtmlNode>(ProcessingQueueDefaultCapacity);
            var result = new List<HtmlNode>(ResultListDefaultCapacity);

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

            var processingQueue = new Queue<HtmlNode>(ProcessingQueueDefaultCapacity);
            var result = new List<HtmlNode>(ResultListDefaultCapacity);

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