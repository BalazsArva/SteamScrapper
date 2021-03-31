namespace SteamScrapper.Domain.Models
{
    public record Price(decimal NormalPrice, decimal? DiscountPrice, string Currency)
    {
        private const decimal UnknownPriceValue = -1m;

        public static readonly Price Unknown = new(UnknownPriceValue, null, string.Empty);
    }
}