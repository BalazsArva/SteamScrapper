using System;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISteamService
    {
        Task<string> GetPageHtmlAsync(Uri uri);

        Task<TResult> GetJsonAsync<TResult>(Uri uri);
    }
}