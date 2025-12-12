using System.ComponentModel.DataAnnotations;

namespace Plafind.ViewModels
{
    public class PaymentViewModel
    {
        [Required]
        [Display(Name = "Plan Tipi")]
        public string PlanType { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Ödeme Tipi")]
        public string PaymentType { get; set; } = "Subscription";
        
        public int? BusinessId { get; set; }
        
        [Display(Name = "Tutar")]
        public decimal Amount { get; set; }
        
        [Display(Name = "Kart Numarası")]
        [StringLength(19)]
        public string? CardNumber { get; set; }
        
        [Display(Name = "Kart Sahibi")]
        [StringLength(100)]
        public string? CardHolderName { get; set; }
        
        [Display(Name = "Son Kullanma Ay")]
        [StringLength(2)]
        public string? ExpiryMonth { get; set; }
        
        [Display(Name = "Son Kullanma Yıl")]
        [StringLength(4)]
        public string? ExpiryYear { get; set; }
        
        [Display(Name = "CVV")]
        [StringLength(4)]
        public string? Cvv { get; set; }
    }

    public class SubscriptionPlan
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public int DurationDays { get; set; }
        public List<string> Features { get; set; } = new List<string>();
    }
}
