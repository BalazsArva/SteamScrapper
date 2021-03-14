using System;

namespace SteamScrapper.Domain.Services.Exceptions
{
    public class SteamPageRemovedException : SteamServiceException
    {
        public SteamPageRemovedException(int statusCode, Uri uri)
            : base($"The page located at {uri?.AbsoluteUri} is no longer accessible. A status code of {statusCode} was received while executing the request.", uri)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; }
    }
}