using System;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ICrawlerAddressRegistrationService
    {
        Task<Uri> GetNextAddressAsync(DateTime executionDate);
    }
}