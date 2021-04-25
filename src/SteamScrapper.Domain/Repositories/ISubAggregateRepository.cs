using System.Collections.Generic;
using System.Threading.Tasks;
using SteamScrapper.Domain.Models.Aggregates;

namespace SteamScrapper.Domain.Repositories
{
    public interface ISubAggregateRepository
    {
        Task StoreSubAggregatesAsync(IEnumerable<Sub> subs);
    }
}