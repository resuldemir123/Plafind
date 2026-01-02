using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Plafind.Data;
using Plafind.Models;
using Plafind.Hubs;

namespace Plafind.Features.Reservations.Services
{
    public class ReservationService : IReservationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ReservationHub> _hubContext;

        public ReservationService(ApplicationDbContext context, IHubContext<ReservationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IEnumerable<Reservation>> GetUserReservationsAsync(string userId)
        {
            return await _context.Reservations
                .Include(r => r.Business)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetAllReservationsAsync()
        {
            return await _context.Reservations
                .Include(r => r.Business)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Business)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reservation> CreateReservationAsync(Reservation reservation, string userId)
        {
            reservation.UserId = userId;
            reservation.Status = "Beklemede";
            reservation.CreatedDate = DateTime.Now;

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // İşletme sahibine anlık bildirim gönder
            var business = await _context.Businesses
                .Include(b => b.Owner)
                .FirstOrDefaultAsync(b => b.Id == reservation.BusinessId);

            if (business != null && !string.IsNullOrEmpty(business.OwnerId))
            {
                var reservationData = new
                {
                    id = reservation.Id,
                    businessId = reservation.BusinessId,
                    businessName = business.Name,
                    requestedDate = reservation.RequestedDate.ToString("dd.MM.yyyy"),
                    requestedTime = reservation.RequestedTime.ToString(@"hh\:mm"),
                    numberOfPeople = reservation.NumberOfPeople,
                    contactPhone = reservation.ContactPhone,
                    contactEmail = reservation.ContactEmail,
                    notes = reservation.Notes,
                    status = reservation.Status,
                    createdDate = reservation.CreatedDate.ToString("dd.MM.yyyy HH:mm")
                };

                await _hubContext.Clients.Group($"business-owner-{business.OwnerId}")
                    .SendAsync("NewReservation", reservationData);
            }

            return reservation;
        }

        public async Task<Reservation> ApproveReservationAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                throw new ArgumentException("Rezervasyon bulunamadı", nameof(id));

            reservation.Status = "Onaylandı";
            reservation.UpdatedDate = DateTime.Now;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<Reservation> RejectReservationAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                throw new ArgumentException("Rezervasyon bulunamadı", nameof(id));

            reservation.Status = "Reddedildi";
            reservation.UpdatedDate = DateTime.Now;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<bool> CancelReservationAsync(int id, string userId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
                return false;

            if (reservation.Status == "Onaylandı")
                return false; // Onaylanmış rezervasyonlar iptal edilemez

            reservation.Status = "İptal";
            reservation.UpdatedDate = DateTime.Now;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUserCancelReservationAsync(int id, string userId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
                return false;

            return reservation.Status != "Onaylandı";
        }
    }
}

