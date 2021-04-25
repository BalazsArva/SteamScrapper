using System.Collections.Generic;

namespace SteamScrapper.Domain.Models.Aggregates
{
    public class PriceHistory
    {
        public string CurrencyName { get; set; }

        public string CurrencySymbol { get; set; }

        public List<PriceHistoryEntry> HistoryEntries { get; } = new List<PriceHistoryEntry>();
    }
}