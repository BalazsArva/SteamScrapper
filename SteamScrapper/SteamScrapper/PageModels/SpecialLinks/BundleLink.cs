using System;
using SteamScrapper.Utilities.LinkHelpers;

namespace SteamScrapper.PageModels.SpecialLinks
{
    public class BundleLink
    {
        public BundleLink(Uri address)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Address = address;
            BundleId = SteamLinkHelper.ExtractBundleId(address);
        }

        public Uri Address { get; }

        public int BundleId { get; }
    }
}