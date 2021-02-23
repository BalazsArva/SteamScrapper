using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ICrawlerAddressRegistrationService
    {
        Task<Uri> GetNextAddressAsync(DateTime executionDate);

        Task<ISet<string>> RegisterNonExploredLinksForExplorationAsync(DateTime executionDate, IEnumerable<Uri> foundLinks);
    }
}