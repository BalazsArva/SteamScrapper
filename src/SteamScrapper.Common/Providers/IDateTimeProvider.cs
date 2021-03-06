using System;

namespace SteamScrapper.Common.Providers
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}