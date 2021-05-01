using System;

namespace SteamScrapper.Infrastructure.Database.Entities
{
    public class BundlePrice
    {
        public long Id { get; set; }

        public long BundleId { get; set; }

        public virtual Bundle Bundle { get; set; }

        public DateTime UtcDateTimeRecorded { get; set; }

        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string Currency { get; set; }
    }
}