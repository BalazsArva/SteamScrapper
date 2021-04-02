using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteamScrapper.Domain.Models;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ICrawlerAddressRegistrationService
    {
        Task<CrawlerExplorationStatistics> GetExplorationStatisticsAsync(DateTime executionDate);

        Task<Uri> GetNextAddressAsync(DateTime executionDate, CancellationToken cancellationToken);

        Task FinalizeExplorationForDateAsync(DateTime executionDate);

        Task CancelReservationsAsync(DateTime executionDate, IEnumerable<Uri> uris);

        Task<ISet<string>> RegisterNonExploredLinksForExplorationAsync(DateTime executionDate, IEnumerable<Uri> foundLinks);
    }
}