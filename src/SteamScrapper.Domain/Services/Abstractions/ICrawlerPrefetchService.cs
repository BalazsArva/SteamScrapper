using System;
using System.Threading.Tasks;
using SteamScrapper.Domain.PageModels;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ICrawlerPrefetchService
    {
        Task<SteamPage> GetNextPageAsync(DateTime utcNow);
    }
}