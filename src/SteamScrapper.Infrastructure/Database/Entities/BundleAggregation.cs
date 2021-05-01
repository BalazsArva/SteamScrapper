using System;

namespace SteamScrapper.Infrastructure.Database.Entities
{
    public class BundleAggregation
    {
        public long Id { get; set; }

        public long BundleId { get; set; }

        public virtual Bundle Bundle { get; set; }

        public DateTime UtcDateTimeRecorded { get; set; }
    }
}