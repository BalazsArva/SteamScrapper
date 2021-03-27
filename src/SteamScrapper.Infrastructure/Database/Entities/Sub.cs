using System;
using System.Collections.Generic;

namespace SteamScrapper.Infrastructure.Database.Entities
{
    public class Sub
    {
        public long Id { get; set; }

        public DateTime UtcDateTimeRecorded { get; set; }

        public DateTime UtcDateTimeLastModified { get; set; }

        public bool IsActive { get; set; }

        public string Title { get; set; }

        public virtual ICollection<SubPrice> Prices { get; } = new List<SubPrice>();
    }
}