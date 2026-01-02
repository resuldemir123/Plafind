using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    /// <summary>
    /// Müşteri etkileşimleri (CRM için)
    /// </summary>
    public class CustomerInteraction
    {
        public int Id { get; set; }
        
        [Required]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        [StringLength(200)]
        public string? InteractionType { get; set; } // "Rezervasyon", "Yorum", "İletişim", "Şikayet", vb.
        
        [StringLength(2000)]
        public string? Notes { get; set; }
        
        public DateTime InteractionDate { get; set; } = DateTime.Now;
        
        [StringLength(200)]
        public string? CreatedBy { get; set; } // İşletme sahibi veya admin ID
    }
}
