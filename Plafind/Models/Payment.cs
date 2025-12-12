using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class Payment
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        public int? BusinessId { get; set; }
        public Business? Business { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PaymentType { get; set; } = string.Empty; // Subscription, Premium, Feature
        
        [Required]
        [StringLength(50)]
        public string PlanType { get; set; } = string.Empty; // Basic, Premium, Enterprise
        
        [Required]
        public decimal Amount { get; set; }
        
        [StringLength(10)]
        public string Currency { get; set; } = "TRY";
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded
        
        [StringLength(200)]
        public string? TransactionId { get; set; }
        
        [StringLength(200)]
        public string? PaymentProvider { get; set; } // iyzico, PayTR, Stripe
        
        [StringLength(500)]
        public string? PaymentMethod { get; set; } // CreditCard, BankTransfer, etc.
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? PaymentDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        public string? Metadata { get; set; } // JSON data
    }

    public class Subscription
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        public int? BusinessId { get; set; }
        public Business? Business { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PlanType { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartDate { get; set; } = DateTime.Now;
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Expired, Cancelled
        
        public bool AutoRenew { get; set; } = true;
        
        public DateTime? CancelledDate { get; set; }
        
        [StringLength(500)]
        public string? CancellationReason { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
