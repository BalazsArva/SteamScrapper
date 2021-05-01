using System.Collections.Generic;

namespace SteamScrapper.Domain.Models.Aggregates
{
    public class Bundle
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string BannerUrl { get; set; }

        public bool IsActive { get; set; }

        public List<PriceHistory> PriceHistoryByCurrency { get; } = new List<PriceHistory>();
    }
}