using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SteamScrapper.Extensions
{
    public static class CollectionExtensions
    {
        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> items)
        {
            if (queue is null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                queue.Enqueue(item);
            }
        }

        public static void EnqueueRange<T>(this ConcurrentQueue<T> queue, IEnumerable<T> items)
        {
            if (queue is null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                queue.Enqueue(item);
            }
        }

        public static IEnumerable<IEnumerable<T>> Segmentate<T>(this IEnumerable<T> source, int segmentSize)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (segmentSize < 1)
            {
                throw new ArgumentException($"The value of the '{nameof(segmentSize)}' parameter must be at least 1.", nameof(segmentSize));
            }

            var resultsHolder = new List<T>(segmentSize);

            foreach (var item in source)
            {
                resultsHolder.Add(item);

                if (resultsHolder.Count == segmentSize)
                {
                    yield return resultsHolder;

                    resultsHolder = new List<T>(segmentSize);
                }
            }

            if (resultsHolder.Count > 0)
            {
                yield return resultsHolder;
            }
        }
    }
}