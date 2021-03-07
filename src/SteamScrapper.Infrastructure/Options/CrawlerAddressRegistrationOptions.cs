namespace SteamScrapper.Infrastructure.Options
{
    public class CrawlerAddressRegistrationOptions
    {
        public const string SectionName = "Crawler:AddressRegistration";

        public bool EnableRecordingIgnoredLinks { get; set; }
    }
}