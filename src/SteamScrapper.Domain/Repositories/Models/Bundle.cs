namespace SteamScrapper.Domain.Repositories.Models
{
    public record Bundle(long BundleId, string Title, string BannerUrl, bool IsActive);
}