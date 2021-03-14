using System;

namespace SteamScrapper.Domain.Services.Exceptions
{
    public class SteamServiceException : Exception
    {
        public SteamServiceException(string message, Uri uri)
            : base(message)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public SteamServiceException(string message, Uri uri, Exception innerException)
            : base(message, innerException)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public SteamServiceException(Uri uri, Exception innerException)
            : base($"An unhandled exception occurred during the execution of a Steam request to URI {uri?.AbsoluteUri}.", innerException)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public Uri Uri { get; }
    }
}