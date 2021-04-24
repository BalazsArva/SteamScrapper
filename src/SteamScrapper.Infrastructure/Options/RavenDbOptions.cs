namespace SteamScrapper.Infrastructure.Options
{
    public class RavenDbOptions
    {
        public const string SectionName = "RavenDb";

        public string Database { get; set; }

        public string[] ServerUrls { get; set; }
    }
}