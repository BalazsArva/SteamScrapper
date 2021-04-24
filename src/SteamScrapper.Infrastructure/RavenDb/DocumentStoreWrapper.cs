using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using SteamScrapper.Infrastructure.Options;

namespace SteamScrapper.Infrastructure.RavenDb
{
    public class DocumentStoreWrapper : IDocumentStoreWrapper
    {
        private readonly Lazy<IDocumentStore> documentStoreLazy;

        public DocumentStoreWrapper(IOptions<RavenDbOptions> ravenDbOptions)
        {
            if (ravenDbOptions?.Value is null)
            {
                throw new ArgumentNullException(nameof(ravenDbOptions));
            }

            if (string.IsNullOrWhiteSpace(ravenDbOptions.Value.Database))
            {
                throw new ArgumentException("The provided configuration object does not contain a valid database name.", nameof(ravenDbOptions));
            }

            if (ravenDbOptions.Value.ServerUrls.Length == 0 || ravenDbOptions.Value.ServerUrls.Any(x => string.IsNullOrWhiteSpace(x)))
            {
                throw new ArgumentException(
                    "The provided configuration object must contain at least 1 server URL and none of the URLs may be null, empty, or whitespace-only.",
                    nameof(ravenDbOptions));
            }

            documentStoreLazy = new Lazy<IDocumentStore>(() =>
            {
                return new DocumentStore
                {
                    Database = ravenDbOptions.Value.Database,
                    Urls = ravenDbOptions.Value.ServerUrls,
                }.Initialize();
            });
        }

        public IDocumentStore DocumentStore => documentStoreLazy.Value;
    }
}