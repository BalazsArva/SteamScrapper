using System;

namespace SteamScrapper.Domain.Services.Exceptions
{
    public class SteamRateLimitExceededException : SteamServiceException
    {
        public SteamRateLimitExceededException(Uri uri)
            : base("The request rate limit has been exceeded. Try executing the request later.", uri)
        {
        }

        public SteamRateLimitExceededException(string message, Uri uri)
            : base(message, uri)
        {
        }

        public SteamRateLimitExceededException(string message, Uri uri, Exception innerException)
            : base(message, uri, innerException)
        {
        }
    }
}