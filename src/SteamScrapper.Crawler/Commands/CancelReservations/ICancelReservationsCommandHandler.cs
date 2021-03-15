using System.Threading.Tasks;

namespace SteamScrapper.Crawler.Commands.CancelReservations
{
    public interface ICancelReservationsCommandHandler
    {
        Task CancelReservations();
    }
}