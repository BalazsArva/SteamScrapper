namespace SteamScrapper.Domain.Services.Contracts
{
    public record BundleData(long BundleId, string Title, string BannerUrl, bool IsActive);
}