using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    /// <summary>
    /// Müşteri bilgileri (CRM için)
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        
        [Required]
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        [StringLength(200)]
        public string? UserId { get; set; } // ApplicationUser ile ilişki (eğer kayıtlı kullanıcı ise)
        public ApplicationUser? User { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Email { get; set; }
        
        [StringLength(50)]
        public string? Phone { get; set; }
        
        [StringLength(50)]
        public string? Segment { get; set; } // Aile, Çift, İş Seyahati, Yabancı Turist, vb.
        
        [StringLength(500)]
        public string? Tags { get; set; } // VIP, Sorunlu, Tekrar Gelen, vb. (JSON array olarak saklanabilir)
        
        [StringLength(2000)]
        public string? Notes { get; set; } // Genel notlar
        
        public int TotalReservations { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0;
        public DateTime? LastVisitDate { get; set; }
        public DateTime? FirstVisitDate { get; set; }
        
        public bool IsVIP { get; set; } = false;
        public bool IsReturning { get; set; } = false;
        public bool HasIssues { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        
        // Navigation properties
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<CustomerInteraction> Interactions { get; set; } = new List<CustomerInteraction>();
    }
}

