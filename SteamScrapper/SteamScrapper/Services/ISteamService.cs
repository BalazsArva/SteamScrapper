using System;
using System.Threading.Tasks;

namespace SteamScrapper.Services
{
    public interface ISteamService
    {
        Task<string> GetPageHtmlAsync(Uri uri);

        Task<TResult> GetJsonAsync<TResult>(Uri uri);
    }
}