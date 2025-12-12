using System;

namespace Plafind.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public string? UserId { get; set; } // Nullable olarak işaretlendi
        public int? BranchId { get; set; } // Şube bilgisi
        public DateTime ReservationDate { get; set; } = DateTime.Now;
        public DateTime RequestedDate { get; set; }
        public TimeSpan RequestedTime { get; set; }
        public int NumberOfPeople { get; set; }
        public string? Status { get; set; } = "Beklemede";
        public string? Notes { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public string? AdminNotes { get; set; }
        public decimal? Amount { get; set; } // Rezervasyon tutarı (gelir takibi için)

        // Navigation properties
        public Business? Business { get; set; }
        public ApplicationUser? User { get; set; }
        public Branch? Branch { get; set; }
    }
}