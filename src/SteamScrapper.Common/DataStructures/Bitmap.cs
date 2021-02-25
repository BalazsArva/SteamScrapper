using System;

namespace SteamScrapper.Common.DataStructures
{
    public class Bitmap
    {
        private const int bucketSize = sizeof(ulong);

        private readonly ulong[] buckets;
        private readonly int size;

        public Bitmap(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentException($"The '{nameof(capacity)}' parameter must be at least 1.", nameof(capacity));
            }

            var bucketCount = capacity / bucketSize;

            size = capacity;
            buckets = new ulong[bucketCount];
        }

        public void Set(int index, bool bit)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"The '{nameof(index)}' parameter must be at least 0.");
            }

            if (index >= size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"The '{nameof(index)}' parameter must not exceed the bitmap's capacity.");
            }

            var bucketIndex = index / bucketSize;
            var bucketOffset = index % bucketSize;

            if (bit)
            {
                var mask = ((ulong)1) << bucketOffset;

                buckets[bucketIndex] |= mask;
            }
            else
            {
                var mask1 = ((ulong)1) << bucketOffset;
                var mask2 = ulong.MaxValue;
                var mask = mask1 ^ mask2;

                buckets[bucketIndex] &= mask;
            }
        }

        public bool Get(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException($"The '{nameof(index)}' parameter must be at least 0.", nameof(index));
            }

            if (index >= size)
            {
                throw new ArgumentOutOfRangeException($"The '{nameof(index)}' parameter must not exceed the bitmap's capacity.", nameof(index));
            }

            var bucketIndex = index / bucketSize;
            var bucketOffset = index % bucketSize;

            var value = buckets[bucketIndex];

            return ((value >> bucketOffset) & 1) != 0;
        }
    }
}