using System;

namespace SteamScrapper.Crawler.Options
{
    public class RegisterStartingAddressesOptions
    {
        public const string SectionName = "RegisterStartingAddresses";

        public Uri[] StartingAddresses { get; set; }
    }
}