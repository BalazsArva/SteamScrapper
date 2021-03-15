using System;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Services.Abstractions
{
    public interface ISteamService
    {
        Task<string> GetPageHtmlWithoutRetryAsync(Uri uri);

        Task<TResult> GetJsonAsync<TResult>(Uri uri);
    }
}