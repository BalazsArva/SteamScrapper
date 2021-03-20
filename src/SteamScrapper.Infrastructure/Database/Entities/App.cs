using System;

namespace SteamScrapper.Infrastructure.Database.Entities
{
    public class App
    {
        public long Id { get; set; }

        public DateTime UtcDateTimeRecorded { get; set; }

        public DateTime UtcDateTimeLastModified { get; set; }

        public bool IsActive { get; set; }

        public string Title { get; set; }

        public string BannerUrl { get; set; }
    }
}