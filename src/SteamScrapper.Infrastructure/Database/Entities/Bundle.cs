using System;
using System.Collections.Generic;

namespace SteamScrapper.Infrastructure.Database.Entities
{
    public class Bundle
    {
        public long Id { get; set; }

        public DateTime UtcDateTimeRecorded { get; set; }

        public DateTime UtcDateTimeLastModified { get; set; }

        public bool IsActive { get; set; }

        public string Title { get; set; }

        public string BannerUrl { get; set; }

        public virtual ICollection<BundlePrice> Prices { get; } = new List<BundlePrice>();
    }
}