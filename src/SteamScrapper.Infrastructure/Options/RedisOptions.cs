namespace SteamScrapper.Infrastructure.Options
{
    public class RedisOptions
    {
        public const string SectionName = "Redis";

        public string ConnectionString { get; set; }
    }
}