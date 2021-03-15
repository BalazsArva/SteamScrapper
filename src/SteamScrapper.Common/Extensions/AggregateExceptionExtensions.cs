using System;
using System.Collections.Generic;

namespace SteamScrapper.Common.Extensions
{
    public static class AggregateExceptionExtensions
    {
        public static IEnumerable<Exception> Unwrap(this AggregateException aggregateException)
        {
            if (aggregateException is null)
            {
                throw new ArgumentNullException(nameof(aggregateException));
            }

            var result = new List<Exception>(50);
            var processingQueue = new Queue<Exception>(50);

            while (processingQueue.Count > 0)
            {
                var nextItem = processingQueue.Dequeue();

                if (nextItem is AggregateException nextAggregateException)
                {
                    processingQueue.EnqueueRange(nextAggregateException.InnerExceptions);
                }
                else
                {
                    result.Add(nextItem);
                }
            }

            return result;
        }
    }
}