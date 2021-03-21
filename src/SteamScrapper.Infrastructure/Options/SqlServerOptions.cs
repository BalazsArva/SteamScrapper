namespace SteamScrapper.Infrastructure.Options
{
    public class SqlServerOptions
    {
        public const string SectionName = "SqlServer";

        public string ConnectionString { get; set; }
    }
}