using System;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISteamService
    {
        Task<string> GetHtmlAsync(Uri uri);

        Task<TResult> GetJsonAsync<TResult>(Uri uri);
    }
}