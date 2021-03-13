using System.Threading;
using System.Threading.Tasks;

namespace SteamScrapper.Crawler.Commands.RegisterStartingAddresses
{
    public interface IRegisterStartingAddressesCommandHandler
    {
        Task RegisterStartingAddressesAsync(CancellationToken cancellationToken);
    }
}