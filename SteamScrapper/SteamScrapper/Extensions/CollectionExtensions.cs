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
    }
}