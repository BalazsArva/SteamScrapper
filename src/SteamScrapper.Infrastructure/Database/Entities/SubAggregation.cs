using System;

namespace SteamScrapper.Infrastructure.Database.Entities
{
    public class SubAggregation
    {
        public long Id { get; set; }

        public long SubId { get; set; }

        public virtual Sub Sub { get; set; }

        public DateTime UtcDateTimeRecorded { get; set; }
    }
}