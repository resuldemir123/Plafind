using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Type { get; set; } = "Info"; // Info, Success, Warning, Error
        
        [StringLength(50)]
        public string Category { get; set; } = "General"; // Reservation, Review, Payment, System, etc.
        
        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [StringLength(500)]
        public string? ActionUrl { get; set; }
        
        [StringLength(100)]
        public string? ActionText { get; set; }
        
        public int? RelatedEntityId { get; set; }
        
        [StringLength(50)]
        public string? RelatedEntityType { get; set; } // Business, Reservation, Review, etc.
    }

    public class NotificationPreference
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        public bool EmailEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = false;
        public bool PushEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;
        
        // Kategori bazlı tercihler
        public bool ReservationNotifications { get; set; } = true;
        public bool ReviewNotifications { get; set; } = true;
        public bool PaymentNotifications { get; set; } = true;
        public bool SystemNotifications { get; set; } = true;
        public bool MarketingNotifications { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
