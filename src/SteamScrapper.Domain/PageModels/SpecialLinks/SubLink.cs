﻿using System;
using SteamScrapper.Common.Utilities.Links;

namespace SteamScrapper.Domain.PageModels.SpecialLinks
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

        public long SubId { get; }
    }
}