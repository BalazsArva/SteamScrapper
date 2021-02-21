using System;
using System.Threading.Tasks;
using SteamScrapper.PageModels;
using SteamScrapper.Services;

namespace SteamScrapper.Factories
{
    public class SteamPageFactory : ISteamPageFactory
    {
        public const string DeveloperListPageAddress = "https://store.steampowered.com/developer/";
        private readonly ISteamService steamService;

        public SteamPageFactory(ISteamService steamService)
        {
            this.steamService = steamService ?? throw new ArgumentNullException(nameof(steamService));
        }

        public async Task<SteamPage> CreateSteamPageAsync(Uri uri)
        {
            if (string.Equals(DeveloperListPageAddress, uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
            {
                return await DeveloperListPage.CreateAsync(steamService);
            }
            else
            {
                // TODO: Account for other special types of pages.
                return await SteamPage.CreateAsync(uri.AbsoluteUri);
            }
        }
    }
}