using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    /// <summary>
    /// Sezonluk fiyatlandırma ve paket fiyatları
    /// </summary>
    public class Pricing
    {
        public int Id { get; set; }
        
        [Required]
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // Örn: "Yaz Sezonu", "Kış Paketi", "Hafta Sonu"
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [Required]
        public decimal BasePrice { get; set; } // Temel fiyat
        
        public decimal? PricePerPerson { get; set; } // Kişi başı fiyat
        public decimal? PricePerNight { get; set; } // Gece başı fiyat (otel için)
        public decimal? PricePerHour { get; set; } // Saat başı fiyat (tur için)
        
        [StringLength(50)]
        public string? PricingType { get; set; } // "PerPerson", "PerNight", "PerHour", "Fixed"
        
        public int? MinPeople { get; set; } // Minimum kişi sayısı
        public int? MaxPeople { get; set; } // Maksimum kişi sayısı
        
        public decimal? WeekendPrice { get; set; } // Hafta sonu fiyatı
        public decimal? HolidayPrice { get; set; } // Tatil günü fiyatı
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}

