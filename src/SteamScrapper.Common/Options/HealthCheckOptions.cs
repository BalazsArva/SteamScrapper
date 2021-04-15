using System;

namespace SteamScrapper.Common.Options
{
    public class HealthCheckOptions
    {
        public const string SectionName = "HealthCheck";

        public const int DefaultMaxUnhealthyCount = 3;

        public static readonly TimeSpan DefaultHealthCheckPeriod = TimeSpan.FromSeconds(30);

        public int MaxUnhealthyCount { get; set; } = DefaultMaxUnhealthyCount;

        public TimeSpan HealthCheckPeriod { get; set; } = DefaultHealthCheckPeriod;
    }
}