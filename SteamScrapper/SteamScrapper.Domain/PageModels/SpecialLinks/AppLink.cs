using System;
using SteamScrapper.Common.Utilities.Links;

namespace SteamScrapper.Domain.PageModels.SpecialLinks
{
    public class AppLink
    {
        public AppLink(Uri address)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Address = address;
            AppId = SteamLinkHelper.ExtractAppId(address);
        }

        public Uri Address { get; }

        public int AppId { get; }
    }
}