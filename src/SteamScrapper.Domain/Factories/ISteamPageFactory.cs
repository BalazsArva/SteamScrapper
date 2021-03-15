using System;
using System.Threading.Tasks;
using SteamScrapper.Domain.PageModels;

namespace SteamScrapper.Domain.Factories
{
    public interface ISteamPageFactory
    {
        Task<SteamPage> CreateSteamPageAsync(Uri uri);

        Task<AppPage> CreateAppPageAsync(int appId);

        Task<SubPage> CreateSubPageAsync(int subId);

        Task<BundlePage> CreateBundlePageAsync(int bundleId);
    }
}