using System.Collections.Generic;

namespace SteamScrapper.Domain.Models.Aggregates
{
    public class Sub
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public bool IsActive { get; set; }

        public Dictionary<string, List<PriceHistoryEntry>> PriceHistoryByCurrency { get; } = new Dictionary<string, List<PriceHistoryEntry>>();
    }
}