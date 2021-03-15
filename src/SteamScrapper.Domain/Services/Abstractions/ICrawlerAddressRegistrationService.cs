using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ICrawlerAddressRegistrationService
    {
        Task<Uri> GetNextAddressAsync(DateTime executionDate, CancellationToken cancellationToken);

        Task UndoReservationsAsync(DateTime executionDate, IEnumerable<Uri> uris);

        Task<ISet<string>> RegisterNonExploredLinksForExplorationAsync(DateTime executionDate, IEnumerable<Uri> foundLinks);
    }
}