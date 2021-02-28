using System;
using System.Threading.Tasks;
using SteamScrapper.Domain.PageModels;

namespace SteamScrapper.Domain.Factories
{
    public interface ISteamPageFactory
    {
        Task<SteamPage> CreateSteamPageAsync(Uri uri, string pageHtml);

        Task<SteamPage> CreateSteamPageAsync(Uri uri);

        Task<AppPage> CreateAppPageAsync(int appId);
    }
}