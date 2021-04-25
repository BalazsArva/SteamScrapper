using System;

namespace SteamScrapper.Domain.Models.Aggregates
{
    public class PriceHistoryEntry
    {
        public decimal NormalPrice { get; set; }

        public decimal? DiscountPrice { get; set; }

        public DateTime UtcDateTimeRecorded { get; set; }
    }
}