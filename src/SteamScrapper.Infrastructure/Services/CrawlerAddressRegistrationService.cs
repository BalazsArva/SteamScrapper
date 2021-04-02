using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using SteamScrapper.Common.Urls;
using SteamScrapper.Domain.Models;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.Redis;

namespace SteamScrapper.Infrastructure.Services
{
    public class CrawlerAddressRegistrationService : ICrawlerAddressRegistrationService
    {
        private const string RedisDateStampFormat = "yyyyMMdd";

        private static readonly IEnumerable<string> LinksAllowedForExploration = new HashSet<string>
        {
            PageUrls.SteamStore,
            PageUrls.Linux,
            PageUrls.MacOS,
        };

        private static readonly IEnumerable<string> LinkPrefixesAllowedForExploration = new[]
        {
            PageUrlPrefixes.App,
            PageUrlPrefixes.Bundle,
            PageUrlPrefixes.Controller,
            PageUrlPrefixes.Demos,
            PageUrlPrefixes.Developer,
            PageUrlPrefixes.Dlc,
            PageUrlPrefixes.Explore,
            PageUrlPrefixes.Franchise,
            PageUrlPrefixes.Games,
            PageUrlPrefixes.Genre,
            PageUrlPrefixes.Publisher,
            PageUrlPrefixes.Recommended,
            PageUrlPrefixes.Sale,
            PageUrlPrefixes.Specials,
            PageUrlPrefixes.Sub,
            PageUrlPrefixes.Tags,
        };

        private readonly IDatabase redisDatabase;
        private readonly bool EnableRecordingIgnoredLinks;

        public CrawlerAddressRegistrationService(
            ILogger<CrawlerAddressRegistrationService> logger,
            IRedisConnectionWrapper redisConnectionWrapper,
            IOptions<CrawlerAddressRegistrationOptions> options)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (redisConnectionWrapper is null)
            {
                throw new ArgumentNullException(nameof(redisConnectionWrapper));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Value is null)
            {
                throw new ArgumentException(
                    "The provided configuration object does not contain valid settings for crawler address registration.",
                    nameof(options));
            }

            EnableRecordingIgnoredLinks = options.Value.EnableRecordingIgnoredLinks;
            redisDatabase = redisConnectionWrapper.ConnectionMultiplexer.GetDatabase();

            logger.LogInformation("Recording ignored links is {@EnableRecordingIgnoredLinksInfoText}.", EnableRecordingIgnoredLinks ? "enabled" : "disabled");
        }

        public async Task<CrawlerExplorationStatistics> GetExplorationStatisticsAsync(DateTime executionDate)
        {
            var redisKeyDateStamp = executionDate.ToString(RedisDateStampFormat);
            var redisTransaction = redisDatabase.CreateTransaction();

            var toBeExploredCountTask = redisTransaction.SetLengthAsync(GetToBeExploredSetName(redisKeyDateStamp));
            var exploredCountTask = redisTransaction.SetLengthAsync(GetExploredSetName(redisKeyDateStamp));

            await redisTransaction.ExecuteAsync();

            return new CrawlerExplorationStatistics(await exploredCountTask, await toBeExploredCountTask);
        }

        public async Task<Uri> GetNextAddressAsync(DateTime executionDate, CancellationToken cancellationToken)
        {
            var redisKeyDateStamp = executionDate.ToString(RedisDateStampFormat);
            var toBeExploredSetName = GetToBeExploredSetName(redisKeyDateStamp);

            while (!cancellationToken.IsCancellationRequested)
            {
                var addressToProcessAbsUris = await redisDatabase.SetPopAsync(toBeExploredSetName, 1);
                var addressToProcessAbsUri = addressToProcessAbsUris.Select(x => (string)x).ElementAtOrDefault(0)?.Trim();

                if (string.IsNullOrWhiteSpace(addressToProcessAbsUri))
                {
                    // Ran out of items to process
                    return null;
                }

                if (await TryRegisterLinkAsExploredAsync(redisKeyDateStamp, addressToProcessAbsUri))
                {
                    return new Uri(addressToProcessAbsUri, UriKind.Absolute);
                }
            }

            return null;
        }

        public async Task CancelReservationsAsync(DateTime executionDate, IEnumerable<Uri> uris)
        {
            if (uris is null)
            {
                throw new ArgumentNullException(nameof(uris));
            }

            if (!uris.Any())
            {
                return;
            }

            if (uris.Any(x => string.IsNullOrWhiteSpace(x.AbsoluteUri)))
            {
                throw new ArgumentException($"All provided URIs must have their {nameof(Uri.AbsoluteUri)} values set.", nameof(uris));
            }

            var redisKeyDateStamp = executionDate.ToString(RedisDateStampFormat);
            var exploredSetName = GetExploredSetName(redisKeyDateStamp);
            var toBeExploredSetName = GetToBeExploredSetName(redisKeyDateStamp);

            var transaction = redisDatabase.CreateTransaction();

            foreach (var uri in uris)
            {
                var absoluteUri = uri.AbsoluteUri;

                _ = transaction.SetRemoveAsync(exploredSetName, absoluteUri);
                _ = transaction.SetAddAsync(toBeExploredSetName, absoluteUri);
            }

            await transaction.ExecuteAsync();
        }

        public async Task<ISet<string>> RegisterNonExploredLinksForExplorationAsync(DateTime executionDate, IEnumerable<Uri> foundLinks)
        {
            if (foundLinks is null)
            {
                throw new ArgumentNullException(nameof(foundLinks));
            }

            if (!foundLinks.Any())
            {
                return new HashSet<string>();
            }

            var redisKeyDateStamp = executionDate.ToString(RedisDateStampFormat);
            var toBeExploredLinks = foundLinks.Where(uri => IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
            var helperSetName = GetHelperSetName(redisKeyDateStamp, Guid.NewGuid().ToString("n"));

            var updateExplorationStatusTransaction = redisDatabase.CreateTransaction();

            var addToBeExploredToHelperSetTask = updateExplorationStatusTransaction.SetAddAsync(helperSetName, toBeExploredLinks);
            if (EnableRecordingIgnoredLinks)
            {
                var ignoredLinks = foundLinks.Where(uri => !IsLinkAllowedForExploration(uri)).Select(uri => new RedisValue(uri.AbsoluteUri)).ToArray();
                var addIgnoredLinksTask = updateExplorationStatusTransaction.SetAddAsync(GetIgnoredSetName(redisKeyDateStamp), ignoredLinks);
            }

            var computeNonexploredLinks = updateExplorationStatusTransaction.SetCombineAndStoreAsync(SetOperation.Difference, helperSetName, helperSetName, GetExploredSetName(redisKeyDateStamp));

            var notYetExploredRedisValsTask = updateExplorationStatusTransaction.SetMembersAsync(helperSetName);

            var deleteHelperSetTask = updateExplorationStatusTransaction.KeyDeleteAsync(helperSetName);

            await updateExplorationStatusTransaction.ExecuteAsync();

            var notYetExploredLinks = notYetExploredRedisValsTask.Result.Select(x => (string)x).ToHashSet();
            if (notYetExploredLinks.Count != 0)
            {
                await redisDatabase.SetAddAsync(GetToBeExploredSetName(redisKeyDateStamp), notYetExploredRedisValsTask.Result);
            }

            return notYetExploredLinks;
        }

        private static bool IsLinkAllowedForExploration(Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri;

            if (absoluteUri.StartsWith(PageUrlPrefixes.Publisher) || absoluteUri.StartsWith(PageUrlPrefixes.Developer))
            {
                if (uri.Segments.DefaultIfEmpty(string.Empty).Last().Trim('/') == "about")
                {
                    return false;
                }
            }

            return
                LinksAllowedForExploration.Contains(absoluteUri) ||
                LinkPrefixesAllowedForExploration.Any(x => absoluteUri.StartsWith(x));
        }

        private async Task<bool> TryRegisterLinkAsExploredAsync(string redisKeyDateStamp, string addressToProcessAbsoluteUri)
        {
            return await redisDatabase.SetAddAsync(GetExploredSetName(redisKeyDateStamp), addressToProcessAbsoluteUri);
        }

        private static string GetExploredSetName(string redisKeyDateStamp) => $"Crawler:{redisKeyDateStamp}:Explored";

        private static string GetToBeExploredSetName(string redisKeyDateStamp) => $"Crawler:{redisKeyDateStamp}:ToBeExplored";

        private static string GetIgnoredSetName(string redisKeyDateStamp) => $"Crawler:{redisKeyDateStamp}:Ignored";

        private static string GetHelperSetName(string redisKeyDateStamp, string helperSetId) => $"Crawler:{redisKeyDateStamp}:HelperSets:{helperSetId}";
    }
}