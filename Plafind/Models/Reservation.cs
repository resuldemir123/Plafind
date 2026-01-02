using System;
using System.ComponentModel.DataAnnotations;

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
        
        // Yeni alanlar - İşletme sahibi paneli için
        [StringLength(500)]
        public string? Tags { get; set; } // VIP, Sorunlu, Tekrar Gelen, vb. (JSON array)
        
        [StringLength(2000)]
        public string? OwnerNotes { get; set; } // İşletme sahibi notları
        
        public decimal? PrePaymentAmount { get; set; } // Ön ödeme tutarı
        public decimal? RemainingAmount { get; set; } // Kalan tutar
        public bool IsPrePaymentReceived { get; set; } = false;
        public bool IsFullPaymentReceived { get; set; } = false;
        
        public DateTime? CheckInDate { get; set; } // Check-in tarihi
        public TimeSpan? CheckInTime { get; set; } // Check-in saati
        public DateTime? CheckOutDate { get; set; } // Check-out tarihi
        public TimeSpan? CheckOutTime { get; set; } // Check-out saati
        public DateTime? TourStartTime { get; set; } // Tur başlangıç saati (tur işletmeleri için)
        public DateTime? TourEndTime { get; set; } // Tur bitiş saati
        
        public bool IsNoShow { get; set; } = false; // No-show işareti
        public DateTime? NoShowDate { get; set; }
        
        [StringLength(50)]
        public string? Channel { get; set; } // Rezervasyon kanalı: "Website", "Phone", "Walk-in", "Partner"
        
        [StringLength(100)]
        public string? PackageName { get; set; } // Paket adı (varsa)
        
        public int? CustomerId { get; set; } // Customer tablosu ile ilişki
        public Customer? Customer { get; set; }
        
        [StringLength(2000)]
        public string? SpecialRequests { get; set; } // Özel istekler

        // Navigation properties
        public Business? Business { get; set; }
        public ApplicationUser? User { get; set; }
        public Branch? Branch { get; set; }
    }
}