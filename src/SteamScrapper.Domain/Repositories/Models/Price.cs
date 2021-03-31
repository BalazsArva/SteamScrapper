namespace SteamScrapper.Domain.Repositories.Models
{
    public record Price(decimal Value, decimal? DiscountValue, string Currency);
}