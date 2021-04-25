using System.Collections.Generic;

namespace SteamScrapper.Domain.Models.Aggregates
{
    public class Sub
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public bool IsActive { get; set; }

        public List<PriceHistory> PriceHistoryByCurrency { get; } = new List<PriceHistory>();
    }
}