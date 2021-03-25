using System;

namespace SteamScrapper.Common.DataStructures
{
    public class Bitmap
    {
        private const int bucketSize = 64;

        private readonly ulong[] buckets;
        private readonly int size;

        public Bitmap(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentException($"The '{nameof(capacity)}' parameter must be at least 1.", nameof(capacity));
            }

            var bucketCount = (int)(Math.Ceiling(1M * capacity / bucketSize));

            size = capacity;
            buckets = new ulong[bucketCount];
        }

        public void Set(long index, bool bit)
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
            var bucketOffset = (int)(index % bucketSize);

            if (bit)
            {
                var mask = 1UL << bucketOffset;

                buckets[bucketIndex] |= mask;
            }
            else
            {
                // ~: bitwise complement
                var mask = ~(((ulong)1) << bucketOffset);

                buckets[bucketIndex] &= mask;
            }
        }

        public bool Get(long index)
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
            var bucketOffset = (int)(index % bucketSize);

            var value = buckets[bucketIndex];

            return ((value >> bucketOffset) & 1) != 0;
        }
    }
}