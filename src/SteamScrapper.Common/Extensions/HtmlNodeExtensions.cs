using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using SteamScrapper.Common.Html;

namespace SteamScrapper.Common.Extensions
{
    public static class HtmlNodeExtensions
    {
        public static bool HasAttribute(this HtmlNode htmlNode, string attributeName, string attributeValue)
        {
            return htmlNode.Attributes.Any(x => x.Name == attributeName && x.Value == attributeValue);
        }

        public static bool HasAttribute(this HtmlNode htmlNode, string attributeName, HtmlAttributeValueTypes attributeValueType = HtmlAttributeValueTypes.None)
        {
            return htmlNode.Attributes.Any(x =>
            {
                if (x.Name != attributeName)
                {
                    return false;
                }

                if ((attributeValueType & HtmlAttributeValueTypes.NotEmpty) != 0 && string.IsNullOrWhiteSpace(x.Value))
                {
                    return false;
                }

                if ((attributeValueType & HtmlAttributeValueTypes.AbsoluteUri) != 0 && !Uri.TryCreate(x.Value, UriKind.Absolute, out var _))
                {
                    return false;
                }

                return true;
            });
        }

        public static Dictionary<string, IEnumerable<HtmlNode>> GetDescendantsByNames(this HtmlNode htmlNode, params string[] elementNames)
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