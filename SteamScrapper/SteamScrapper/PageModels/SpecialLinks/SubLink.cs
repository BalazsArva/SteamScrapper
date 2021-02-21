using System;
using SteamScrapper.Utilities.LinkHelpers;

namespace SteamScrapper.PageModels.SpecialLinks
{
    public class SubLink
    {
        public SubLink(Uri address)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Address = address;
            SubId = SteamLinkHelper.ExtractSubId(address);
        }

        public Uri Address { get; }

        public int SubId { get; }
    }
}