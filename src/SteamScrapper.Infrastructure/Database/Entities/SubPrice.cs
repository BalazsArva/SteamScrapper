using System;

namespace SteamScrapper.Infrastructure.Database.Entities
{
    public class SubPrice
    {
        public long Id { get; set; }

        public long SubId { get; set; }

        public virtual Sub Sub { get; set; }

        public DateTime UtcDateTimeRecorded { get; set; }

        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string Currency { get; set; }
    }
}