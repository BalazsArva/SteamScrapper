using System;
using System.Threading.Tasks;
using SteamScrapper.Domain.PageModels;

namespace SteamScrapper.Domain.Factories
{
    public interface ISteamPageFactory
    {
        Task<SteamPage> CreateSteamPageAsync(Uri uri);

        Task<AppPage> CreateAppPageAsync(long appId);

        Task<SubPage> CreateSubPageAsync(long subId);

        Task<BundlePage> CreateBundlePageAsync(long bundleId);
    }
}