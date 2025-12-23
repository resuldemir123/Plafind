using Plafind.Models;

namespace Plafind.Features.Reservations.Services
{
    public interface IReservationService
    {
        Task<IEnumerable<Reservation>> GetUserReservationsAsync(string userId);
        Task<IEnumerable<Reservation>> GetAllReservationsAsync();
        Task<Reservation?> GetReservationByIdAsync(int id);
        Task<Reservation> CreateReservationAsync(Reservation reservation, string userId);
        Task<Reservation> ApproveReservationAsync(int id);
        Task<Reservation> RejectReservationAsync(int id);
        Task<bool> CancelReservationAsync(int id, string userId);
        Task<bool> CanUserCancelReservationAsync(int id, string userId);
    }
}

