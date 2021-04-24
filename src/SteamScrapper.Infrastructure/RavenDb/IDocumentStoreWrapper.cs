using Raven.Client.Documents;

namespace SteamScrapper.Infrastructure.RavenDb
{
    public interface IDocumentStoreWrapper
    {
        IDocumentStore DocumentStore { get; }
    }
}