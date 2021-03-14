using System;

namespace SteamScrapper.Domain.Services.Exceptions
{
    public class SteamUnexpectedStatusCodeException : SteamServiceException
    {
        public SteamUnexpectedStatusCodeException(int statusCode, Uri uri)
            : base($"An unexpected status code of {statusCode} has been received while executing the request to URI {uri?.AbsoluteUri}.", uri)
        {
            StatusCode = statusCode;
        }

        public SteamUnexpectedStatusCodeException(string message, int statusCode, Uri uri)
            : base(message, uri)
        {
            StatusCode = statusCode;
        }

        public SteamUnexpectedStatusCodeException(string message, int statusCode, Uri uri, Exception innerException)
            : base(message, uri, innerException)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; }
    }
}