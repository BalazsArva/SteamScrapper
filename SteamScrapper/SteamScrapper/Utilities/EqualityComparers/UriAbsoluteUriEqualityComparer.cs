using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SteamScrapper.Utilities.EqualityComparers
{
    public class UriAbsoluteUriEqualityComparer : IEqualityComparer<Uri>
    {
        public static UriAbsoluteUriEqualityComparer Instance { get; } = new UriAbsoluteUriEqualityComparer();

        public bool Equals(Uri x, Uri y)
        {
            return string.Equals(x?.AbsoluteUri, y?.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] Uri uri)
        {
            return uri.AbsoluteUri.GetHashCode();
        }
    }
}