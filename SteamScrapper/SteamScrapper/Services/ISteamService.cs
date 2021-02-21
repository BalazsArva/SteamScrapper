using System;
using System.Threading.Tasks;
using SteamScrapper.PageModels;

namespace SteamScrapper.Services
{
    public interface ISteamService
    {
        Task<SteamPage> CreateSteamPageAsync(Uri uri);

        Task<string> DownloadPageHtmlAsync(Uri uri);

        Task<TResult> GetJsonAsync<TResult>(Uri uri);
    }
}