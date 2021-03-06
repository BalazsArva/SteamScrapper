using System;

namespace SteamScrapper.Common.Html
{
    [Flags]
    public enum HtmlAttributeValueTypes
    {
        None = 0,
        NotEmpty = 1,
        AbsoluteUri = 2,
    }
}