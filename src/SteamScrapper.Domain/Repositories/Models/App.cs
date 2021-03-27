namespace SteamScrapper.Domain.Repositories.Models
{
    public record App(long AppId, string Title, string BannerUrl, bool IsActive);
}