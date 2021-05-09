using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams;
using StackExchange.Redis;
using SteamScrapper.Common.Providers;

namespace SteamScrapper.Infrastructure.Redis
{
    public class RedisCache
    {
        private readonly IRedisConnectionWrapper redisConnectionWrapper;
        private readonly IDateTimeProvider dateTimeProvider;

        public RedisCache(IRedisConnectionWrapper redisConnectionWrapper, IDateTimeProvider dateTimeProvider)
        {
            this.redisConnectionWrapper = redisConnectionWrapper ?? throw new ArgumentNullException(nameof(redisConnectionWrapper));
            this.dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task SetAsync(string key, string value, DateTime expiration)
        {
            var utcNow = dateTimeProvider.UtcNow;

            if (utcNow >= expiration)
            {
                throw new ArgumentException($"The value of the '{nameof(expiration)}' parameter must be in the future.", nameof(expiration));
            }

            await SetAsync(key, value, expiration - utcNow);
        }

        public async Task SetAsync(string key, string value, TimeSpan timeToLive)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"The parameter '{nameof(key)}' cannot be null, empty or whitespace-only.", nameof(key));
            }

            var valueBytes = Encoding.UTF8.GetBytes(value);
            var compressedBytes = Compress(valueBytes);

            await redisConnectionWrapper.ConnectionMultiplexer.GetDatabase().StringSetAsync(key, compressedBytes, timeToLive, When.Always);
        }

        public async Task<string> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"The parameter '{nameof(key)}' cannot be null, empty or whitespace-only.", nameof(key));
            }

            var valueBytes = await redisConnectionWrapper.ConnectionMultiplexer.GetDatabase().StringGetAsync(key);

            if (valueBytes.HasValue)
            {
                var decompressed = Decompress(valueBytes);

                return Encoding.UTF8.GetString(decompressed);
            }

            return null;
        }

        private static byte[] Compress(byte[] sourceBytes)
        {
            var outputStream = new MemoryStream(sourceBytes.Length);

            using (var sourceStream = new MemoryStream(sourceBytes))
            using (var compressionSteam = LZ4Stream.Encode(outputStream, K4os.Compression.LZ4.LZ4Level.L12_MAX))
            {
                sourceStream.CopyTo(compressionSteam);
            }

            return outputStream.ToArray();
        }

        private static byte[] Decompress(byte[] sourceBytes)
        {
            var outputStream = new MemoryStream(sourceBytes.Length * 4);

            using (var sourceStream = new MemoryStream(sourceBytes))
            using (var decompressionSteam = LZ4Stream.Decode(outputStream))
            {
                decompressionSteam.CopyTo(outputStream);
            }

            return outputStream.ToArray();
        }
    }
}