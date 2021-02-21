using System;
using System.Threading.Tasks;
using SteamScrapper.PageModels;

namespace SteamScrapper.Utilities.Factories
{
    public interface ISteamPageFactory
    {
        Task<SteamPage> CreateSteamPageAsync(Uri uri);
    }
}